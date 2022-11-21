using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MCH.API.Models;
using MCH.Data;
using MCH.Data.Entities;
using MCH.Parset.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCH.Core.Parsing
{
    public class ParsingRepository: IParsingRepository
    {
        private readonly ProductionInfoDbContext _context;
        private readonly ILogger<ParsingRepository> _logger;
        public ParsingRepository(ProductionInfoDbContext context, ILogger<ParsingRepository> logger)
        {
            _logger = logger;
            _context = context;
        }
        /// <summary>
        /// Добавление товара
        /// </summary>
        /// <param name="product"></param>
        public async Task AddProductAsync(ProductEntity productEntity)
        {
            _logger.LogInformation($"Adding product withName: {productEntity.ProductName}. CompanyId: {productEntity.CompanyId}");
            await _context.ProductEntities.AddAsync(productEntity);
            }

        /// <summary>
        /// Добавление товара, если его еще нет.
        /// Если есть - то обновление
        /// </summary>
        /// <param name="productEntity"></param>
        public async Task AddOrUpdateProductAsync(ProductEntity productEntity)
        {
            _logger.LogInformation($"Add or update product withName: {productEntity.ProductName}. CompanyId: {productEntity.CompanyId}");
            var product = _context.ProductEntities
                .FirstOrDefault(p => p.CompanyId == productEntity.CompanyId
                                     && p.Url == productEntity.Url);
            if (product is null)
            {
                _logger.LogInformation($"Adding new product. url: {productEntity.Url}. CompanyId: {productEntity.CompanyId}");
                await _context.ProductEntities.AddAsync(productEntity);
            }
            else
            {
                _logger.LogInformation($"Product with url: {productEntity.Url} already exists. Update");
                product.ProductName = productEntity.ProductName;
                product.Description = productEntity.Description;
                product.Price = productEntity.Price;
            }
        }

        
        /// <summary>
        /// Добавление компании-производителя
        /// </summary>
        /// <param name="company"></param>
        public async Task AddCompanyAsync(CompanyEntity company)
        {
            _logger.LogInformation($"Adding company with name: {company.CompanyName}");
            await _context.CompanyEntities.AddAsync(company);
        }
       

        /// <summary>
        /// Получение списка всех компаний
        /// </summary>
        /// <param name="count">Максимально количество производителей в ответе</param>
        /// <param name="offset">Смещение </param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public IEnumerable<CompanyEntity> getCompanies(int count, int offset)
        {
            _logger.LogInformation($"Getting top {count} companies with offset:{offset}");
            var result =
                _context.CompanyEntities.FromSqlRaw(
                    $"SELECT * FROM \"CompanyEntities\"  LIMIT {count} OFFSET {offset}");
            return result;
        }
        
        /// <summary>
        /// Получение списка всех компаний
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CompanyEntity> getAllCompanies()
        {
            _logger.LogInformation("Getting all companies");
            return _context.CompanyEntities;
        }
        
        /// <summary>
        /// Получение списка ссылок для парсинга
        /// для компании
        /// </summary>
        /// <param name="companyId">Id компании</param>
        /// <returns></returns>
        public IEnumerable<UrlsToParseEntity> getUrlsToParse(int companyId)
        {
            _logger.LogInformation($"Getting urls to parse for company with Id: {companyId}");
            return _context.UrlsToParseEntities
                .Where(u => u.CompanyId == companyId);
        }

        /// <summary>
        /// Получение объекта компании
        /// </summary>
        /// <param name="Id">Id компании</param>
        /// <returns></returns>
        public CompanyEntity getCompanyEntity(int Id)
        {
            _logger.LogInformation($"Getting company with Id: {Id}");
            return _context.CompanyEntities.First(c => c.Id == Id);
        }

        /// <summary>
        /// Добавление изображения продукта
        /// </summary>
        /// <param name="image"></param>
        public async Task AddImageIfNotExistAsync(ImageEntity image)
        {
            var imageInDb = _context.ImageEntities.FirstOrDefault(i => i.Url == image.Url && i.ProductId == image.ProductId);
            _logger.LogInformation($"Trying add new image to produc: {image.ProductId}. Url: {image.Url}");
            if (imageInDb is  null)
            {
                await _context.ImageEntities.AddAsync(image);
            }
            else
            {
                _logger.LogInformation($"Image with url: {image.Url} and productId: {image.ProductId} already exists");
            }
           
           
        }

        /// <summary>
        /// Вовращает список товаров из общего списка, без ограничений
        /// на компани, категорию и тд
        /// </summary>
        /// <param name="count">Количетсво товаров</param>
        /// <param name="offset">Смещение</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ProductsListResponse GetProducts(int count, int offset)
        {
            _logger.LogInformation($"Getting top {count} product with offset:{offset}");
            var result = new ProductsListResponse();
            result.products =  _context.ProductEntities.FromSqlRaw($"SELECT * FROM \"ProductEntities\"  LIMIT {count} OFFSET {offset}")
                .Include(p => p.Images);
            result.TotalProjects =   _context.ProductEntities.FromSqlRaw($"SELECT * FROM \"ProductEntities\"").Count();
            return result;
        }


        /// <summary>
        /// Получение списка товаров компании
        /// </summary>
        /// <param name="companyId">Id компании</param>
        /// <param name="count">Максимальное количество продуктов</param>
        /// <param name="offset">Количество пропускаемых сообщений (смещение)</param>
        /// <returns></returns>
        public ProductsListResponse GetProductsByCompany(int companyId, int count, int offset)
        {
            _logger.LogInformation($"Getting top {count} product of company with id: {companyId}");
            var result = new ProductsListResponse();
           result.products =  _context.ProductEntities.FromSqlRaw($"SELECT * FROM \"ProductEntities\" WHERE \"CompanyId\" = {companyId} LIMIT {count} OFFSET {offset}")
               .Include(p => p.Images);
           result.TotalProjects =   _context.ProductEntities.FromSqlRaw($"SELECT * FROM \"ProductEntities\" WHERE \"CompanyId\" = {companyId}").Count();
           return result;
        }

        /// <summary>
        /// Получение списка товаров по запросу
        /// </summary>
        /// <param name="query">Запрос</param>
        /// <param name="count">Максимальное количество продуктов</param>
        /// <param name="offset">Количество пропускаемых сообщений (смещение)</param>
        /// <returns></returns>
        public ProductsListResponse GetProductsbyQuery(string query, int count, int offset)
        {
            _logger.LogInformation($"Getting top {count} product for query: {query}");
            var result = new ProductsListResponse();
            result.products = _context.ProductEntities
                .FromSqlRaw($"SELECT * FROM \"ProductEntities\" LIMIT {count} OFFSET {offset}")
                .Include(p => p.Images);
            result.TotalProjects =   _context.ProductEntities.FromSqlRaw($"SELECT  * FROM \"ProductEntities\"").Count();
            return result;
        }
        

        /// <summary>
        /// Добавление ссылки для паринга
        /// </summary>
        /// <param name="urlsToParse"></param>
        public async Task AddUrlToParseAsync(UrlsToParseEntity urlsToParse)
        {
            _logger.LogInformation($"Adding url to parse: {urlsToParse.Url}. CompanyId: {urlsToParse.CompanyId}");
            await _context.UrlsToParseEntities.AddAsync(urlsToParse);
        }

        public int CountProducts(int companyId)
        {
            _logger.LogInformation($"Getting count products of company:{companyId}");
            return _context.ProductEntities
                .Count(p => p.CompanyId == companyId);
        }

        public IEnumerable<ProductEntity> GetProductById(int productId)
        {
            _logger.LogInformation($"Getting product with Id: {productId}");
            return _context.ProductEntities
                .Include(p => p.Images)
                .Where(p => p.Id == productId);
        }

        
        /// <summary>
        /// Возвращает список товаров по набоку Id 
        /// </summary>
        /// <param name="productIds">Список Id товаров</param>
        /// <returns></returns>
        public IEnumerable<ProductEntity> GetProductsByListId(ProductIds productIds)
        {
            _logger.LogInformation($"Getting products with Ids:{string.Join(", ",productIds?.Ids)}");
            return _context.ProductEntities
                .Include(p => p.Images)
                .Where(p => productIds.Ids.Contains(p.Id));
        }

        public CompanyEntity GetCompanyByIIN(string IIN)
        {
            return _context.CompanyEntities
                .FirstOrDefault(c => c.IIN == IIN);
        }
    }
}