angular.module('virtoCommerce.catalogModule')
.factory('virtoCommerce.catalogModule.items', ['$resource', function ($resource) {
    return $resource('api/catalog/products/:id', null, {
        remove: { method: 'DELETE', url: 'api/catalog/products' },
        newItemInCatalog: { method: 'GET', url: 'api/catalog/:catalogId/products/getnew' },
        newItemInCategory: { method: 'GET', url: 'api/catalog/:catalogId/categories/:categoryId/products/getnew' },
        newVariation: { method: 'GET', url: 'api/catalog/products/:itemId/getnewvariation' },
        cloneItem: { method: 'GET', url: 'api/catalog/products/:itemId/clone' },
        update: { method: 'POST' }
    });
}])
.factory('virtoCommerce.catalogModule.ingredientsService', ['$q', '$filter', function ($q, $filter) {
      function ingredientsService(searchCriteria) {
          var productIngredients = { "totalCount": 1, "productIngredients": [{"id":"1","supplierName":"Supplier1","ammount":50.00, "measure":"mg", "sortOrder":1, "ingredientId":"1"}]};
          var ingredients = { "totalCount": 2, "ingredients": [{"id":"1","name": "Name1","supplierName":"Supplier1"},{"id":"2","name": "Name2","supplierName":"Supplier2"}]};
          var self = this;
          var fakeHttpCall = function (isSuccessful) {
              var deferred = $q.defer()
              if (isSuccessful === true) {
                  deferred.resolve("yeap!")
              }
              else {
                  deferred.reject("Oh no! Something went terribly wrong in you fake $http call")
              }
              return deferred.promise
          }
          self.getProductIngredientList = function(searchCreteria) {
              return fakeHttpCall(true).then(
                  function(data) {
                      return productIngredients;
                  },
                  function(err) {
                      // error callback
                      console.log(err)
                  });
          }
          
          self.getIngredientList = function(searchCreteria) {
              return fakeHttpCall(true).then(
                  function(data) {
                      return ingredients;
                  },
                  function(err) {
                      // error callback
                      console.log(err)
                  });
          }

      }
    return new ingredientsService();
}]);

