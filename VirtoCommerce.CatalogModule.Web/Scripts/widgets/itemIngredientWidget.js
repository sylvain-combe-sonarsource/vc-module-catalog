angular.module('virtoCommerce.catalogModule')
.controller('virtoCommerce.catalogModule.itemIngredientWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
    var blade = $scope.blade;

    $scope.openVariationListBlade = function () {
        var newBlade = {
            id: "itemProductIngredientList",
            item: blade.item,
            catalog: blade.catalog,
            //toolbarCommandsAndEvents: blade.variationsToolbarCommandsAndEvents,
            controller: 'virtoCommerce.catalogModule.itemProductIngredientListController',
            template: 'Modules/$(VirtoCommerce.Catalog)/Scripts/blades/item-product-ingredient-list.tpl.html',
        };
        bladeNavigationService.showBlade(newBlade, blade);
    };
}]);
