angular.module('virtoCommerce.catalogModule')
.controller('virtoCommerce.catalogModule.itemIngredientListController', ['$scope', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'virtoCommerce.catalogModule.items', 'filterFilter', 'uiGridConstants', 'platformWebApp.uiGridHelper', function ($scope, bladeNavigationService, dialogService, items, filterFilter, uiGridConstants, uiGridHelper) {
    $scope.uiGridConstants = uiGridConstants;
    var blade = $scope.blade;

    //pagination settings
    $scope.pageSettings = {};
    $scope.pageSettings.totalItems = 0;
    $scope.pageSettings.currentPage = 1;
    $scope.pageSettings.numPages = 5;
    $scope.pageSettings.itemsPerPageCount = 20;

    blade.isLoading = false;

    blade.refresh = function (item) {
    	if (item) {
    		initialize(item);
    	}
    	else {
    		blade.parentBlade.refresh();
    	}

    };

    function initialize(item) {
    	blade.title = item.name;
    	blade.subtitle = 'catalog.widgets.itemVariation.blade-subtitle';
    	blade.item = item;
    	$scope.pageSettings.totalItems = blade.item.variations.length;
    }

    

    // ui-grid
    $scope.setGridOptions = function (gridOptions) {
        uiGridHelper.initialize($scope, gridOptions,
        function (gridApi) {
            gridApi.grid.registerRowsProcessor($scope.singleFilter, 90);
            $scope.$watch('pageSettings.currentPage', gridApi.pagination.seek);

            if (blade.toolbarCommandsAndEvents && blade.toolbarCommandsAndEvents.externalRegisterApiCallback) {
                blade.toolbarCommandsAndEvents.externalRegisterApiCallback(gridApi);
            }
        });
    };

    initialize(blade.item);
}]);
