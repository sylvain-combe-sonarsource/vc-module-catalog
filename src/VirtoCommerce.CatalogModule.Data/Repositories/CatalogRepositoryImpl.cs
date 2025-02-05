using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Data.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Data.Extensions;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.CatalogModule.Data.Repositories
{
    public class CatalogRepositoryImpl : DbContextRepositoryBase<CatalogDbContext>, ICatalogRepository
    {
        private const int batchSize = 500;

        public CatalogRepositoryImpl(CatalogDbContext dbContext)
            : base(dbContext)
        {
        }

        #region ICatalogRepository Members

        public IQueryable<CategoryEntity> Categories => DbContext.Set<CategoryEntity>();
        public IQueryable<CatalogEntity> Catalogs => DbContext.Set<CatalogEntity>();
        public IQueryable<PropertyValueEntity> PropertyValues => DbContext.Set<PropertyValueEntity>();
        public IQueryable<ImageEntity> Images => DbContext.Set<ImageEntity>();
        public IQueryable<AssetEntity> Assets => DbContext.Set<AssetEntity>();
        public IQueryable<ItemEntity> Items => DbContext.Set<ItemEntity>();
        public IQueryable<EditorialReviewEntity> EditorialReviews => DbContext.Set<EditorialReviewEntity>();
        public IQueryable<PropertyEntity> Properties => DbContext.Set<PropertyEntity>();
        public IQueryable<PropertyDictionaryItemEntity> PropertyDictionaryItems => DbContext.Set<PropertyDictionaryItemEntity>();
        public IQueryable<PropertyDictionaryValueEntity> PropertyDictionaryValues => DbContext.Set<PropertyDictionaryValueEntity>();
        public IQueryable<PropertyDisplayNameEntity> PropertyDisplayNames => DbContext.Set<PropertyDisplayNameEntity>();
        public IQueryable<PropertyAttributeEntity> PropertyAttributes => DbContext.Set<PropertyAttributeEntity>();
        public IQueryable<CategoryItemRelationEntity> CategoryItemRelations => DbContext.Set<CategoryItemRelationEntity>();
        public IQueryable<AssociationEntity> Associations => DbContext.Set<AssociationEntity>();
        public IQueryable<CategoryRelationEntity> CategoryLinks => DbContext.Set<CategoryRelationEntity>();
        public IQueryable<PropertyValidationRuleEntity> PropertyValidationRules => DbContext.Set<PropertyValidationRuleEntity>();
        public IQueryable<SeoInfoEntity> SeoInfos => DbContext.Set<SeoInfoEntity>();

        public virtual async Task<CatalogEntity[]> GetCatalogsByIdsAsync(string[] catalogIds)
        {
            var result = Array.Empty<CatalogEntity>();

            if (!catalogIds.IsNullOrEmpty())
            {
                result = await Catalogs.Include(x => x.CatalogLanguages)
                    .Include(x => x.IncomingLinks)
                    .Where(x => catalogIds.Contains(x.Id))
                    .ToArrayAsync();

                if (result.Any())
                {
                    //https://docs.microsoft.com/en-us/ef/core/querying/async

                    await PropertyValues.Include(x => x.DictionaryItem.DictionaryItemValues)
                                                           .Where(x => catalogIds.Contains(x.CatalogId) && x.CategoryId == null).LoadAsync();
                    var catalogPropertiesIds = await Properties.Where(x => catalogIds.Contains(x.CatalogId) && x.CategoryId == null)
                        .Select(x => x.Id)
                        .ToArrayAsync();
                    await GetPropertiesByIdsAsync(catalogPropertiesIds);
                }
            }
            return result;
        }

        public virtual async Task<CategoryEntity[]> GetCategoriesByIdsAsync(string[] categoriesIds, string responseGroup)
        {
            var categoryResponseGroup = EnumUtility.SafeParseFlags(responseGroup, CategoryResponseGroup.Full);
            var result = Array.Empty<CategoryEntity>();

            if (!categoriesIds.IsNullOrEmpty())
            {
                result = await Categories.Where(x => categoriesIds.Contains(x.Id)).ToArrayAsync();

                if (result.Any())
                {
                    if (categoryResponseGroup.HasFlag(CategoryResponseGroup.WithOutlines))
                    {
                        categoryResponseGroup |= CategoryResponseGroup.WithLinks | CategoryResponseGroup.WithParents;
                    }

                    if (categoryResponseGroup.HasFlag(CategoryResponseGroup.WithLinks))
                    {
                        await CategoryLinks.Where(x => categoriesIds.Contains(x.TargetCategoryId)).LoadAsync();
                        await CategoryLinks.Where(x => categoriesIds.Contains(x.SourceCategoryId)).LoadAsync();
                    }

                    if (categoryResponseGroup.HasFlag(CategoryResponseGroup.WithImages))
                    {
                        await Images.Where(x => categoriesIds.Contains(x.CategoryId)).LoadAsync();
                    }

                    if (categoryResponseGroup.HasFlag(CategoryResponseGroup.WithSeo))
                    {
                        await SeoInfos.Where(x => categoriesIds.Contains(x.CategoryId)).LoadAsync();
                    }

                    //Load all properties meta information and information for inheritance
                    if (categoryResponseGroup.HasFlag(CategoryResponseGroup.WithProperties))
                    {
                        await PropertyValues.Include(x => x.DictionaryItem.DictionaryItemValues).Where(x => categoriesIds.Contains(x.CategoryId)).LoadAsync();
                        //Load category property values by separate query
                        await PropertyValues.Include(x => x.DictionaryItem.DictionaryItemValues)
                                                               .Where(x => categoriesIds.Contains(x.CategoryId)).LoadAsync();

                        var categoryPropertiesIds = await Properties.Where(x => categoriesIds.Contains(x.CategoryId))
                                                                    .Select(x => x.Id).ToArrayAsync();
                        await GetPropertiesByIdsAsync(categoryPropertiesIds);
                    }
                }
            }

            return result;
        }

        public virtual async Task<ItemEntity[]> GetItemByIdsAsync(string[] itemIds, string responseGroup = null)
        {
            var itemResponseGroup = EnumUtility.SafeParseFlags(responseGroup, ItemResponseGroup.ItemLarge);
            var result = Array.Empty<ItemEntity>();

            if (!itemIds.IsNullOrEmpty())
            {
                // Use breaking query EF performance concept https://docs.microsoft.com/en-us/ef/ef6/fundamentals/performance/perf-whitepaper#8-loading-related-entities
                result = await Items.Include(x => x.Images).Where(x => itemIds.Contains(x.Id)).ToArrayAsync();

                if (result.Any())
                {
                    if (itemResponseGroup.HasFlag(ItemResponseGroup.Outlines))
                    {
                        itemResponseGroup |= ItemResponseGroup.Links;
                    }

                    if (itemResponseGroup.HasFlag(ItemResponseGroup.ItemProperties))
                    {
                        await PropertyValues.Include(x => x.DictionaryItem.DictionaryItemValues).Where(x => itemIds.Contains(x.ItemId)).LoadAsync();
                    }

                    if (itemResponseGroup.HasFlag(ItemResponseGroup.Links))
                    {
                        await CategoryItemRelations.Where(x => itemIds.Contains(x.ItemId)).LoadAsync();
                    }

                    if (itemResponseGroup.HasFlag(ItemResponseGroup.ItemAssets))
                    {
                        await Assets.Where(x => itemIds.Contains(x.ItemId)).LoadAsync();
                    }

                    if (itemResponseGroup.HasFlag(ItemResponseGroup.ItemEditorialReviews))
                    {
                        await EditorialReviews.Where(x => itemIds.Contains(x.ItemId)).LoadAsync();
                    }

                    if (itemResponseGroup.HasFlag(ItemResponseGroup.WithSeo))
                    {
                        await SeoInfos.Where(x => itemIds.Contains(x.ItemId)).LoadAsync();
                    }

                    if (itemResponseGroup.HasFlag(ItemResponseGroup.Variations))
                    {
                        // TODO: Call GetItemByIds for variations recursively (need to measure performance and data amount first)
                        IQueryable<ItemEntity> variationsQuery = Items.Where(x => itemIds.Contains(x.ParentId))
                                                    .Include(x => x.Images)
                                                    .Include(x => x.ItemPropertyValues).ThenInclude(x => x.DictionaryItem.DictionaryItemValues);

                        if (itemResponseGroup.HasFlag(ItemResponseGroup.ItemAssets))
                        {
                            variationsQuery = variationsQuery.Include(x => x.Assets);
                        }
                        if (itemResponseGroup.HasFlag(ItemResponseGroup.ItemEditorialReviews))
                        {
                            variationsQuery = variationsQuery.Include(x => x.EditorialReviews);
                        }
                        if (itemResponseGroup.HasFlag(ItemResponseGroup.Seo))
                        {
                            variationsQuery = variationsQuery.Include(x => x.SeoInfos);
                        }
                        await variationsQuery.LoadAsync();
                    }

                    if (itemResponseGroup.HasFlag(ItemResponseGroup.ItemAssociations))
                    {
                        var associations = await Associations.Where(x => itemIds.Contains(x.ItemId)).ToArrayAsync();
                        var associatedProductIds = associations.Where(x => x.AssociatedItemId != null)
                            .Select(x => x.AssociatedItemId).Distinct().ToArray();
                        var associatedCategoryIds = associations.Where(x => x.AssociatedCategoryId != null).Select(x => x.AssociatedCategoryId).Distinct().ToArray();

                        await GetItemByIdsAsync(associatedProductIds, (ItemResponseGroup.ItemInfo | ItemResponseGroup.ItemAssets).ToString());
                        await GetCategoriesByIdsAsync(associatedCategoryIds, (CategoryResponseGroup.Info | CategoryResponseGroup.WithImages).ToString());
                    }

                    if (itemResponseGroup.HasFlag(ItemResponseGroup.ReferencedAssociations))
                    {
                        var referencedAssociations = await Associations.Where(x => itemIds.Contains(x.AssociatedItemId)).ToArrayAsync();
                        var referencedProductIds = referencedAssociations.Select(x => x.ItemId).Distinct().ToArray();
                        await GetItemByIdsAsync(referencedProductIds, ItemResponseGroup.ItemInfo.ToString());
                    }

                    // Load parents
                    var parentIds = result.Where(x => x.Parent == null && x.ParentId != null).Select(x => x.ParentId).ToArray();
                    await GetItemByIdsAsync(parentIds, responseGroup);
                }
            }

            return result;
        }

        public virtual async Task<PropertyEntity[]> GetPropertiesByIdsAsync(string[] propIds, bool loadDictValues = false)
        {
            var result = Array.Empty<PropertyEntity>();

            if (!propIds.IsNullOrEmpty())
            {
                //Used breaking query EF performance concept https://msdn.microsoft.com/en-us/data/hh949853.aspx#8
                result = await Properties.Where(x => propIds.Contains(x.Id))
                                         .Include(x => x.PropertyAttributes)
                                         .Include(x => x.DisplayNames)
                                         .Include(x => x.ValidationRules)
                                         .ToArrayAsync();

                if (result.Any() && loadDictValues)
                {
                    await PropertyDictionaryItems.Include(x => x.DictionaryItemValues).Where(x => propIds.Contains(x.PropertyId)).LoadAsync();
                }
            }
            return result;
        }

        /// <summary>
        /// Returned all properties belongs to specified catalog
        /// For virtual catalog also include all properties for categories linked to this virtual catalog
        /// </summary>
        /// <param name="catalogId"></param>
        /// <returns></returns>
        public virtual async Task<PropertyEntity[]> GetAllCatalogPropertiesAsync(string catalogId)
        {
            var result = Array.Empty<PropertyEntity>();

            if (!string.IsNullOrEmpty(catalogId))
            {
                var catalog = await Catalogs.FirstOrDefaultAsync(x => x.Id == catalogId);

                if (catalog != null)
                {
                    var propertyIds = await Properties.Where(x => x.CatalogId == catalogId).Select(x => x.Id).ToArrayAsync();

                    if (catalog.Virtual)
                    {
                        //get all category relations
                        var linkedCategoryIds = await CategoryLinks.Where(x => x.TargetCatalogId == catalogId)
                            .Select(x => x.SourceCategoryId)
                            .Distinct()
                            .ToArrayAsync();
                        //linked product categories links
                        var linkedProductCategoryIds = await CategoryItemRelations.Where(x => x.CatalogId == catalogId)
                            .Join(Items, link => link.ItemId, item => item.Id, (link, item) => item)
                            .Select(x => x.CategoryId)
                            .Distinct()
                            .ToArrayAsync();
                        linkedCategoryIds = linkedCategoryIds.Concat(linkedProductCategoryIds).Distinct().ToArray();
                        var expandedFlatLinkedCategoryIds = linkedCategoryIds.Concat(await GetAllChildrenCategoriesIdsAsync(linkedCategoryIds)).Distinct().ToArray();

                        propertyIds = propertyIds.Concat(Properties.Where(x => expandedFlatLinkedCategoryIds.Contains(x.CategoryId)).Select(x => x.Id)).Distinct().ToArray();
                        var linkedCatalogIds = await Categories.Where(x => expandedFlatLinkedCategoryIds.Contains(x.Id)).Select(x => x.CatalogId).Distinct().ToArrayAsync();
                        propertyIds = propertyIds.Concat(Properties.Where(x => linkedCatalogIds.Contains(x.CatalogId) && x.CategoryId == null).Select(x => x.Id)).Distinct().ToArray();
                    }

                    result = await GetPropertiesByIdsAsync(propertyIds);
                }
            }

            return result;
        }

        public virtual async Task<PropertyDictionaryItemEntity[]> GetPropertyDictionaryItemsByIdsAsync(string[] dictItemIds)
        {
            var result = Array.Empty<PropertyDictionaryItemEntity>();

            if (!dictItemIds.IsNullOrEmpty())
            {
                result = await PropertyDictionaryItems.Include(x => x.DictionaryItemValues)
                    .Where(x => dictItemIds.Contains(x.Id))
                    .ToArrayAsync();
            }

            return result;
        }

        public virtual async Task<AssociationEntity[]> GetAssociationsByIdsAsync(string[] associationIds)
        {
            var result = Array.Empty<AssociationEntity>();

            if (!associationIds.IsNullOrEmpty())
            {
                result = await Associations.Where(x => associationIds.Contains(x.Id)).ToArrayAsync();
            }

            return result;
        }

        public virtual async Task<string[]> GetAllSeoDuplicatesIdsAsync()
        {
            const string commandTemplate = @"
                    WITH cte AS (
	                    SELECT
		                    Id,
		                    Keyword,
		                    StoreId,
		                    ROW_NUMBER() OVER ( PARTITION BY Keyword, StoreId ORDER BY StoreId) row_num
	                    FROM CatalogSeoInfo
                    )
                    SELECT Id FROM cte
                    WHERE row_num > 1
                ";

            var command = CreateCommand(commandTemplate, new string[0]);
            var result = await DbContext.ExecuteArrayAsync<string>(command.Text, command.Parameters.ToArray());

            return result ?? new string[0];
        }

        public virtual async Task<string[]> GetAllChildrenCategoriesIdsAsync(string[] categoryIds)
        {
            string[] result = null;

            if (!categoryIds.IsNullOrEmpty())
            {
                const string commandTemplate = @"
                    WITH cte AS (
                        SELECT a.Id FROM Category a  WHERE Id IN ({0})
                        UNION ALL
                        SELECT a.Id FROM Category a JOIN cte c ON a.ParentCategoryId = c.Id
                    )
                    SELECT Id FROM cte WHERE Id NOT IN ({0})
                ";

                var getAllChildrenCategoriesCommand = CreateCommand(commandTemplate, categoryIds);
                result = await DbContext.ExecuteArrayAsync<string>(getAllChildrenCategoriesCommand.Text, getAllChildrenCategoriesCommand.Parameters.ToArray());
            }

            return result ?? new string[0];
        }

        public virtual async Task RemoveItemsAsync(string[] itemIds)
        {
            if (!itemIds.IsNullOrEmpty())
            {
                var skip = 0;
                do
                {
                    const string commandTemplate = @"
                        DELETE SEO FROM CatalogSeoInfo SEO INNER JOIN Item I ON I.Id = SEO.ItemId
                        WHERE I.Id IN ({0}) OR I.ParentId IN ({0})

                        DELETE CR FROM CategoryItemRelation  CR INNER JOIN Item I ON I.Id = CR.ItemId
                        WHERE I.Id IN ({0}) OR I.ParentId IN ({0})

                        DELETE CI FROM CatalogImage CI INNER JOIN Item I ON I.Id = CI.ItemId
                        WHERE I.Id IN ({0})  OR I.ParentId IN ({0})

                        DELETE CA FROM CatalogAsset CA INNER JOIN Item I ON I.Id = CA.ItemId
                        WHERE I.Id IN ({0}) OR I.ParentId IN ({0})

                        DELETE PV FROM PropertyValue PV INNER JOIN Item I ON I.Id = PV.ItemId
                        WHERE I.Id IN ({0}) OR I.ParentId IN ({0})

                        DELETE ER FROM EditorialReview ER INNER JOIN Item I ON I.Id = ER.ItemId
                        WHERE I.Id IN ({0}) OR I.ParentId IN ({0})

                        DELETE A FROM Association A INNER JOIN Item I ON I.Id = A.ItemId
                        WHERE I.Id IN ({0}) OR I.ParentId IN ({0})

                        DELETE A FROM Association A INNER JOIN Item I ON I.Id = A.AssociatedItemId
                        WHERE I.Id IN ({0}) OR I.ParentId IN ({0})

                        DELETE  FROM Item  WHERE ParentId IN ({0})

                        DELETE  FROM Item  WHERE Id IN ({0})
                    ";

                    await ExecuteStoreQueryAsync(commandTemplate, itemIds.Skip(skip).Take(batchSize));

                    skip += batchSize;
                }
                while (skip < itemIds.Length);

                //TODO: Notify about removed entities by event or trigger
            }
        }

        public virtual async Task RemoveCategoriesAsync(string[] ids)
        {
            if (!ids.IsNullOrEmpty())
            {
                var categoryIds = (await GetAllChildrenCategoriesIdsAsync(ids)).Concat(ids).ToArray();

                var itemIds = await Items.Where(i => categoryIds.Contains(i.CategoryId)).Select(i => i.Id).ToArrayAsync();
                await RemoveItemsAsync(itemIds);

                var skip = 0;
                do
                {
                    const string commandTemplate = @"
                    DELETE SEO FROM CatalogSeoInfo SEO INNER JOIN Category C ON C.Id = SEO.CategoryId WHERE C.Id IN ({0})
                    DELETE CI FROM CatalogImage CI INNER JOIN Category C ON C.Id = CI.CategoryId WHERE C.Id IN ({0})
                    DELETE PV FROM PropertyValue PV INNER JOIN Category C ON C.Id = PV.CategoryId WHERE C.Id IN ({0})
                    DELETE CR FROM CategoryRelation CR INNER JOIN Category C ON C.Id = CR.SourceCategoryId OR C.Id = CR.TargetCategoryId  WHERE C.Id IN ({0})
                    DELETE CIR FROM CategoryItemRelation CIR INNER JOIN Category C ON C.Id = CIR.CategoryId WHERE C.Id IN ({0})
                    DELETE A FROM Association A INNER JOIN Category C ON C.Id = A.AssociatedCategoryId WHERE C.Id IN ({0})
                    DELETE P FROM Property P INNER JOIN Category C ON C.Id = P.CategoryId  WHERE C.Id IN ({0})
                    DELETE FROM Category WHERE Id IN ({0})
                ";

                    await ExecuteStoreQueryAsync(commandTemplate, categoryIds.Skip(skip).Take(batchSize));

                    skip += batchSize;
                }
                while (skip < categoryIds.Length);

                //TODO: Notify about removed entities by event or trigger
            }
        }

        public virtual async Task RemoveCatalogsAsync(string[] ids)
        {
            if (!ids.IsNullOrEmpty())
            {
                var itemIds = await Items.Where(i => ids.Contains(i.CatalogId)).Select(i => i.Id).ToArrayAsync();
                await RemoveItemsAsync(itemIds);

                var categoryIds = await Categories.Where(c => ids.Contains(c.CatalogId)).Select(c => c.Id).ToArrayAsync();
                await RemoveCategoriesAsync(categoryIds);

                var skip = 0;
                do
                {
                    const string commandTemplate = @"
                    DELETE CL FROM CatalogLanguage CL INNER JOIN Catalog C ON C.Id = CL.CatalogId WHERE C.Id IN ({0})
                    DELETE CR FROM CategoryRelation CR INNER JOIN Catalog C ON C.Id = CR.TargetCatalogId WHERE C.Id IN ({0})
                    DELETE PV FROM PropertyValue PV INNER JOIN Catalog C ON C.Id = PV.CatalogId WHERE C.Id IN ({0})
                    DELETE P FROM Property P INNER JOIN Catalog C ON C.Id = P.CatalogId  WHERE C.Id IN ({0})
                    DELETE FROM Catalog WHERE Id IN ({0})
                ";

                    await ExecuteStoreQueryAsync(commandTemplate, ids.Skip(skip).Take(batchSize));
                    skip += batchSize;
                }
                while (skip < ids.Length);

                //TODO: Notify about removed entities by event or trigger
            }
        }

        /// <summary>
        /// Delete all exist property values belong to given property.
        /// Because PropertyValue table doesn't have a foreign key to Property table by design,
        /// we use columns Name and TargetType to find values that reference to the deleting property.
        /// </summary>
        /// <param name="propertyId"></param>
        public virtual async Task RemoveAllPropertyValuesAsync(string propertyId)
        {
            var properties = await GetPropertiesByIdsAsync(new[] { propertyId });
            var catalogProperty = properties.FirstOrDefault(x => x.TargetType.EqualsInvariant(PropertyType.Catalog.ToString()));
            var categoryProperty = properties.FirstOrDefault(x => x.TargetType.EqualsInvariant(PropertyType.Category.ToString()));
            var itemProperty = properties.FirstOrDefault(x => x.TargetType.EqualsInvariant(PropertyType.Product.ToString()) || x.TargetType.EqualsInvariant(PropertyType.Variation.ToString()));

            string commandText;
            if (catalogProperty != null)
            {
                commandText = $"DELETE PV FROM PropertyValue PV INNER JOIN Catalog C ON C.Id = PV.CatalogId AND C.Id = '{catalogProperty.CatalogId}' WHERE PV.Name = '{catalogProperty.Name}'";
                await DbContext.Database.ExecuteSqlRawAsync(commandText);
            }
            if (categoryProperty != null)
            {
                commandText = $"DELETE PV FROM PropertyValue PV INNER JOIN Category C ON C.Id = PV.CategoryId AND C.CatalogId = '{categoryProperty.CatalogId}' WHERE PV.Name = '{categoryProperty.Name}'";
                await DbContext.Database.ExecuteSqlRawAsync(commandText);
            }
            if (itemProperty != null)
            {
                commandText = $"DELETE PV FROM PropertyValue PV INNER JOIN Item I ON I.Id = PV.ItemId AND I.CatalogId = '{itemProperty.CatalogId}' WHERE PV.Name = '{itemProperty.Name}'";
                await DbContext.Database.ExecuteSqlRawAsync(commandText);
            }
        }

        public virtual async Task<GenericSearchResult<AssociationEntity>> SearchAssociations(ProductAssociationSearchCriteria criteria)
        {
            var result = new GenericSearchResult<AssociationEntity>();

            var countSqlCommand = CreateCommand(GetAssociationsCountSqlCommandText(criteria), criteria.ObjectIds);
            var querySqlCommand = CreateCommand(GetAssociationsQuerySqlCommandText(criteria), criteria.ObjectIds);

            var commands = new List<Command> { countSqlCommand };

            if (criteria.Take > 0)
            {
                commands.Add(querySqlCommand);
            }

            if (!string.IsNullOrEmpty(criteria.Group))
            {
                commands.ForEach(x => x.Parameters.Add(new SqlParameter($"@group", criteria.Group)));
            }

            if (!criteria.Tags.IsNullOrEmpty())
            {
                commands.ForEach(x => AddArrayParameters(x, "@tags", criteria.Tags));
            }

            if (!string.IsNullOrEmpty(criteria.Keyword))
            {
                var wildcardKeyword = $"%{criteria.Keyword}%";
                commands.ForEach(x => x.Parameters.Add(new SqlParameter($"@keyword", wildcardKeyword)));
            }

            if (!criteria.AssociatedObjectIds.IsNullOrEmpty())
            {
                commands.ForEach(x => AddArrayParameters(x, "@associatedoOjectIds", criteria.AssociatedObjectIds));
            }

            result.TotalCount = await DbContext.ExecuteScalarAsync<int>(countSqlCommand.Text, countSqlCommand.Parameters.ToArray());
            result.Results = criteria.Take > 0
                ? await DbContext.Set<AssociationEntity>().FromSqlRaw(querySqlCommand.Text, querySqlCommand.Parameters.ToArray()).ToListAsync()
                : new List<AssociationEntity>();

            return result;
        }

        #endregion ICatalogRepository Members

        protected virtual string GetAssociationsCountSqlCommandText(ProductAssociationSearchCriteria criteria)
        {
            var command = new StringBuilder();

            command.Append(@"
                ;WITH Association_CTE AS
                (
                    SELECT a.*
                    FROM Association a");

            AddAssociationsSearchCriteraToCommand(command, criteria);

            command.Append(@"), Category_CTE AS
                (
                    SELECT AssociatedCategoryId Id
                    FROM Association_CTE
                    WHERE AssociatedCategoryId IS NOT NULL
                    UNION ALL
                    SELECT c.Id
                    FROM Category c
                    INNER JOIN Category_CTE cte ON c.ParentCategoryId = cte.Id
                ),
                Item_CTE AS
                (
                    SELECT  i.Id
                    FROM (SELECT DISTINCT Id FROM Category_CTE) c
                    LEFT JOIN Item i ON c.Id=i.CategoryId WHERE i.ParentId IS NULL
                    UNION
                    SELECT AssociatedItemId Id FROM Association_CTE
                )
                SELECT COUNT(Id) FROM Item_CTE");

            return command.ToString();
        }

        protected virtual string GetAssociationsQuerySqlCommandText(ProductAssociationSearchCriteria criteria)
        {
            var command = new StringBuilder();

            command.Append(@"
                    ;WITH Association_CTE AS
                    (
                        SELECT
                             a.Id
                            ,a.AssociationType
                            ,a.Priority
                            ,a.ItemId
                            ,a.CreatedDate
                            ,a.ModifiedDate
                            ,a.CreatedBy
                            ,a.ModifiedBy
                            ,a.AssociatedItemId
                            ,a.AssociatedCategoryId
                            ,a.Tags
                            ,a.Quantity
                            ,a.OuterId
                        FROM Association a"
            );

            AddAssociationsSearchCriteraToCommand(command, criteria);

            command.Append(@"), Category_CTE AS
                    (
                        SELECT AssociatedCategoryId Id, AssociatedCategoryId
                        FROM Association_CTE
                        WHERE AssociatedCategoryId IS NOT NULL
                        UNION ALL
                        SELECT c.Id, cte.AssociatedCategoryId
                        FROM Category c
                        INNER JOIN Category_CTE cte ON c.ParentCategoryId = cte.Id
                    ),
                    Item_CTE AS
                    (
                        SELECT
                            CONVERT(nvarchar(64), newid()) as Id
                            ,a.AssociationType
                            ,a.Priority
                            ,a.ItemId
                            ,a.CreatedDate
                            ,a.ModifiedDate
                            ,a.CreatedBy
                            ,a.ModifiedBy
                            ,i.Id AssociatedItemId
                            ,a.AssociatedCategoryId
                            ,a.Tags
                            ,a.Quantity
                            ,a.OuterId
                        FROM Category_CTE cat
                        LEFT JOIN Item i ON cat.Id=i.CategoryId
                        LEFT JOIN Association a ON cat.AssociatedCategoryId=a.AssociatedCategoryId
                        WHERE i.ParentId IS NULL
                        UNION
                        SELECT * FROM Association_CTE
                    )
                    SELECT * FROM Item_CTE WHERE AssociatedItemId IS NOT NULL ORDER BY Priority ");

            command.Append($"OFFSET {criteria.Skip} ROWS FETCH NEXT {criteria.Take} ROWS ONLY");

            return command.ToString();
        }

        protected virtual void AddAssociationsSearchCriteraToCommand(StringBuilder command, ProductAssociationSearchCriteria criteria)
        {
            // join items to search by keyword
            if (!string.IsNullOrEmpty(criteria.Keyword))
            {
                command.Append(@"
                    left join Item i on i.Id = a.AssociatedItemId
                ");
            }

            command.Append(@"
                    WHERE ItemId IN ({0})
            ");

            // search by association type
            if (!string.IsNullOrEmpty(criteria.Group))
            {
                command.Append("  AND AssociationType = @group");
            }

            // search by association tags
            if (!criteria.Tags.IsNullOrEmpty())
            {
                command.Append("  AND exists( SELECT value FROM string_split(Tags, ';') WHERE value IN (@tags))");
            }

            // search by keyword
            if (!string.IsNullOrEmpty(criteria.Keyword))
            {
                command.Append("  AND i.Name like @keyword");
            }

            // search by associated product ids
            if (!criteria.AssociatedObjectIds.IsNullOrEmpty())
            {
                command.Append("  AND a.AssociatedItemId in (@associatedoOjectIds)");
            }
        }

        protected virtual async Task<int> ExecuteStoreQueryAsync(string commandTemplate, IEnumerable<string> parameterValues)
        {
            var command = CreateCommand(commandTemplate, parameterValues);
            return await DbContext.Database.ExecuteSqlRawAsync(command.Text, command.Parameters.ToArray());
        }

        protected virtual Command CreateCommand(string commandTemplate, IEnumerable<string> parameterValues)
        {
            var parameters = parameterValues.Select((v, i) => new SqlParameter($"@p{i}", v)).ToArray();
            var parameterNames = string.Join(",", parameters.Select(p => p.ParameterName));

            return new Command
            {
                Text = string.Format(commandTemplate, parameterNames),
                Parameters = parameters.OfType<object>().ToList(),
            };
        }

        protected SqlParameter[] AddArrayParameters<T>(Command cmd, string paramNameRoot, IEnumerable<T> values)
        {
            /* An array cannot be simply added as a parameter to a SqlCommand so we need to loop through things and add it manually.
             * Each item in the array will end up being it's own SqlParameter so the return value for this must be used as part of the
             * IN statement in the CommandText.
             */
            var parameters = new List<SqlParameter>();
            var parameterNames = new List<string>();
            var paramNbr = 1;
            foreach (var value in values)
            {
                var paramName = $"{paramNameRoot}{paramNbr++}";
                parameterNames.Add(paramName);
                var p = new SqlParameter(paramName, value);
                cmd.Parameters.Add(p);
                parameters.Add(p);
            }
            cmd.Text = cmd.Text.Replace(paramNameRoot, string.Join(",", parameterNames));

            return parameters.ToArray();
        }

        protected class Command
        {
            public string Text { get; set; }
            public IList<object> Parameters { get; set; } = new List<object>();
        }
    }
}
