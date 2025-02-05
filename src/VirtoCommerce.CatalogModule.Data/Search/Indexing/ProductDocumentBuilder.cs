using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Extenstions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.CatalogModule.Data.Search.Indexing
{
    public class ProductDocumentBuilder : CatalogDocumentBuilder, IIndexDocumentBuilder
    {
        private readonly IItemService _itemService;
        private readonly IProductSearchService _productsSearchService;

        public ProductDocumentBuilder(ISettingsManager settingsManager, IItemService itemService, IProductSearchService productsSearchService)
            : base(settingsManager)
        {
            _itemService = itemService;
            _productsSearchService = productsSearchService;
        }

        public virtual async Task<IList<IndexDocument>> GetDocumentsAsync(IList<string> documentIds)
        {
            var result = new List<IndexDocument>();
            var products = await GetProducts(documentIds);
            foreach (var product in products)
            {
                var doc = CreateDocument(product);
                result.Add(doc);

                //Index product variants by separate chunked requests for performance reason
                if (product.MainProductId == null)
                {
                    const int pageSize = 50;
                    var variationsSearchCriteria = new Core.Model.Search.ProductSearchCriteria
                    {
                        Take = pageSize,
                        MainProductId = product.Id,
                        ResponseGroup = (ItemResponseGroup.ItemInfo | ItemResponseGroup.Properties | ItemResponseGroup.Seo | ItemResponseGroup.Outlines | ItemResponseGroup.ItemAssets).ToString()
                    };
                    var skipCount = 0;
                    int totalCount;
                    do
                    {
                        variationsSearchCriteria.Skip = skipCount;
                        var productVariations = await _productsSearchService.SearchProductsAsync(variationsSearchCriteria);
                        foreach (var variation in productVariations.Results)
                        {
                            result.Add(CreateDocument(variation));
                            IndexProductVariation(doc, variation);
                        }
                        totalCount = productVariations.TotalCount;
                        skipCount += pageSize;
                    }
                    while (skipCount < totalCount);
                }
            }

            return result;
        }

        protected virtual Task<CatalogProduct[]> GetProducts(IList<string> productIds)
        {
            return _itemService.GetByIdsAsync(productIds.ToArray(), (ItemResponseGroup.Full & ~ItemResponseGroup.Variations).ToString());
        }

        protected virtual IndexDocument CreateDocument(CatalogProduct product)
        {
            var document = new IndexDocument(product.Id);

            document.AddFilterableValue("__type", product.GetType().Name);
            document.AddFilterableValue("__sort", product.Name);

            var statusField = product.IsActive != true || product.MainProductId != null ? "hidden" : "visible";
            IndexIsProperty(document, statusField);
            IndexIsProperty(document, string.IsNullOrEmpty(product.MainProductId) ? "product" : "variation");
            IndexIsProperty(document, product.Code);

            document.AddFilterableValue("status", statusField);
            document.AddFilterableAndSearchableValue("sku", product.Code);
            document.AddFilterableAndSearchableValue("code", product.Code);// { IsRetrievable = true, IsFilterable = true, IsCollection = true });
            document.AddFilterableAndSearchableValue("name", product.Name);
            document.AddFilterableValue("startdate", product.StartDate);
            document.AddFilterableValue("enddate", product.EndDate ?? DateTime.MaxValue);
            document.AddFilterableValue("createddate", product.CreatedDate);
            document.AddFilterableValue("lastmodifieddate", product.ModifiedDate ?? DateTime.MaxValue);
            document.AddFilterableValue("modifieddate", product.ModifiedDate ?? DateTime.MaxValue);
            document.AddFilterableValue("priority", product.Priority);
            document.AddFilterableValue("vendor", product.Vendor ?? string.Empty);
            document.AddFilterableValue("productType", product.ProductType ?? string.Empty);
            document.AddFilterableValue("mainProductId", product.MainProductId ?? string.Empty);
            document.AddFilterableValue("gtin", product.Gtin ?? string.Empty);

            // Add priority in virtual categories to search index
            if (product.Links != null)
            {
                foreach (var link in product.Links)
                {
                    document.AddFilterableValue($"priority_{link.CatalogId}_{link.CategoryId}", link.Priority);
                }
            }

            // Add catalogs to search index
            var catalogs = product.Outlines
                .Select(o => o.Items.First().Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            document.AddFilterableValues("catalog", catalogs);

            // Add outlines to search index
            var outlineStrings = GetOutlineStrings(product.Outlines);
            document.AddFilterableValues("__outline", outlineStrings);

            document.AddFilterableValues("__outline_named", GetOutlineStrings(product.Outlines, getNameLatestItem: true));

            // Add the all physical and virtual paths
            document.AddFilterableValues("__path", product.Outlines.Select(x => string.Join("/", x.Items.Take(x.Items.Count - 1).Select(i => i.Id))).ToList());

            // Types of properties which values should be added to the searchable __content field
            var contentPropertyTypes = new[] { PropertyType.Product, PropertyType.Variation };

            // Index custom product properties
            IndexCustomProperties(document, product.Properties, contentPropertyTypes);

            //Index product category properties
            if (product.Category != null)
            {
                IndexCustomProperties(document, product.Category.Properties, contentPropertyTypes);
            }

            //Index catalog properties
            if (product.Catalog != null)
            {
                IndexCustomProperties(document, product.Catalog.Properties, contentPropertyTypes);
            }

            if (StoreObjectsInIndex)
            {
                // Index serialized product
                document.AddObjectFieldValue(product);
            }

            return document;
        }

        protected virtual void IndexProductVariation(IndexDocument document, CatalogProduct variation)
        {
            if (variation.ProductType == "Physical")
            {
                document.Add(new IndexDocumentField("type", "physical") { IsRetrievable = true, IsFilterable = true, IsCollection = true });
                IndexIsProperty(document, "physical");
            }

            if (variation.ProductType == "Digital")
            {
                document.Add(new IndexDocumentField("type", "digital") { IsRetrievable = true, IsFilterable = true, IsCollection = true });
                IndexIsProperty(document, "digital");
            }

            if (variation.ProductType == "BillOfMaterials")
            {
                document.Add(new IndexDocumentField("type", "billofmaterials") { IsRetrievable = true, IsFilterable = true, IsCollection = true });
                IndexIsProperty(document, "billofmaterials");
            }

            document.Add(new IndexDocumentField("code", variation.Code) { IsRetrievable = true, IsFilterable = true, IsCollection = true });
            // add the variation code to content
            document.Add(new IndexDocumentField("__content", variation.Code) { IsRetrievable = true, IsSearchable = true, IsCollection = true });
            // add the variationId to __variations
            document.Add(new IndexDocumentField("__variations", variation.Id) { IsRetrievable = true, IsSearchable = true, IsCollection = true });

            IndexCustomProperties(document, variation.Properties, new[] { PropertyType.Variation });
        }

        protected virtual void IndexIsProperty(IndexDocument document, string value)
        {
            document.Add(new IndexDocumentField("is", value) { IsRetrievable = true, IsFilterable = true, IsCollection = true });
        }
    }
}
