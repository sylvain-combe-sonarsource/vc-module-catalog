angular.module('virtoCommerce.catalogModule')
.controller('virtoCommerce.catalogModule.itemProductIngredientListController', ['$scope', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'virtoCommerce.catalogModule.ingredientsService', 'virtoCommerce.catalogModule.ingredientValidatorsService', 'filterFilter', 'uiGridConstants', 'platformWebApp.uiGridHelper', 'platformWebApp.ui-grid.extension', 'platformWebApp.objCompareService',
function ($scope, bladeNavigationService, dialogService, ingredients, ingredientValidatorsService, filterFilter, uiGridConstants, uiGridHelper, gridOptionExtension, objCompareService) {
    $scope.uiGridConstants = uiGridConstants;
    var blade = $scope.blade;
    blade.updatePermission = 'pricing:update';
    
    blade.ingredients = [];
    blade.productIngredients = [];
    
    blade.refresh = function () {
        blade.productId = blade.item.id;
        blade.isLoading = true;
        ingredients.getProductIngredientList({id: blade.productId}).then(function(data){
            ingredients.getIngredientList({}).then(function(ingredients){
                blade.ingredients = ingredients.ingredients;    
                if (!blade.ingredients) {
                    blade.ingredients = [];
                }
                blade.selectedIngredient = _.first(blade.ingredients);
            })
            blade.productIngredients = data.productIngredients;    
            if (!blade.productIngredients) {
                blade.productIngredients = [];
            }
            blade.currentEntities = angular.copy(blade.productIngredients);
            blade.origEntity = blade.productIngredients;
            ingredientValidatorsService.setAllPrices(blade.currentEntities);
            blade.isLoading = false;
        });
        
    };
    
    $scope.createIngredient = function () {
        var newBlade = {
            id: 'ingredientList',
            controller: 'virtoCommerce.catalogModule.ingredientListController',
            template: 'Modules/$(VirtoCommerce.catalogModule)/Scripts/blades/ingredient-list.tpl.html',
            title: 'pricing.blades.pricing-main.menu.pricelist-list.title',
            parentRefresh: blade.refresh
        };

        bladeNavigationService.showBlade(newBlade, blade);
    };

    $scope.selectIngredient = function (entity) {
        var newBlade = {
            id: 'listItemChild',
            currentEntityId: entity.id,
            title: entity.name,
            controller: 'virtoCommerce.catalogModule.ingredientDetailController',
            template: 'Modules/$(VirtoCommerce.catalogModule)/Scripts/blades/ingredient-detail.tpl.html'
        };

        bladeNavigationService.showBlade(newBlade, blade);
    };
    
    blade.toolbarCommands = [
        {
            name: "platform.commands.save",
            icon: 'fa fa-save',
            executeMethod: $scope.saveChanges,
            canExecuteMethod: canSave,
            permission: blade.updatePermission
        },
        {
            name: "platform.commands.delete",
            icon: 'fa fa-trash-o',
            executeMethod: function () {
                var selection = $scope.gridApi.selection.getSelectedRows();
                var ids = _.map(selection, function (item) { return item.id; });

                var dialog = {
                    id: "confirmDeleteItem",
                    title: "pricing.dialogs.item-prices-delete.title",
                    message: "pricing.dialogs.item-prices-delete.message",
                    callback: function (remove) {
                        if (remove) {
                            prices.removePrice({ priceIds: ids }, function () {
                                angular.forEach(selection, function (listItem) {
                                    blade.currentEntities.splice(blade.currentEntities.indexOf(listItem), 1);
                                });
                            }, function (error) {
                                bladeNavigationService.setError('Error ' + error.status, blade);
                            });
                        }
                    }
                }
                dialogService.showConfirmationDialog(dialog);
            },
            canExecuteMethod: function () {
                return $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
            },
            permission: 'pricing:delete'
        },
        {
            name: "platform.commands.refresh",
            icon: 'fa fa-refresh',
            executeMethod: blade.refresh,
            canExecuteMethod: function () { return true; }
        }
    ];
    
    blade.addIngredient = function (targetIgredient) {
        //populate prices data for correct work of validation service
        ingredientValidatorsService.setAllPrices(blade.currentEntities);

        var newIngredient = {
            productId: blade.item.id,
            supplierName: targetIgredient.supplierName,
            ammount: 1,
            ingredientId: targetIgredient.id,
            sortOrder: blade.currentEntities.length + 1
        };
        blade.currentEntities.push(newIngredient);
        $scope.validateGridData();
    }
    
    $scope.isMeasureValid = ingredientValidatorsService.isMeasureValid;
    $scope.isAmmountValid = ingredientValidatorsService.isAmmountValid;
    $scope.isUniqueSortOrder = ingredientValidatorsService.isUniqueSortOrder;

    $scope.setForm = function (form) { $scope.formScope = form; }
    
    function isDirty() {
        return blade.currentEntities && !objCompareService.equal(blade.origEntity, blade.currentEntities) && blade.hasUpdatePermission()
    }

    function canSave() {
        return isDirty() && $scope.isValid();
    }

    $scope.isValid = function () {
        return $scope.formScope && $scope.formScope.$valid &&
             _.all(blade.currentEntities, $scope.isMeasureValid) &&
             _.all(blade.currentEntities, $scope.isAmmountValid) &&
             _.all(blade.currentEntities, $scope.isUniqueSortOrder) &&
            (blade.currentEntities.length == 0 || _.some(blade.currentEntities, function (x) { return x.minQuantity == 1; }));
    }
    
    // ui-grid
    $scope.setGridOptions = function (gridId, gridOptions) {
        gridOptions.onRegisterApi = function (gridApi) {
            $scope.gridApi = gridApi;

            gridApi.edit.on.afterCellEdit($scope, function () {
                //to process validation for all rows in grid.
                //e.g. if we have two rows with the same count of min qty, both of this rows will be marked as error.
                //when we change data to valid in one row, another one should became valid too.
                //more info about ui-grid validation: https://github.com/angular-ui/ui-grid/issues/4152
                $scope.validateGridData();
            });

            $scope.validateGridData();
        };

        $scope.gridOptions = gridOptions;
        gridOptionExtension.tryExtendGridOptions(gridId, gridOptions);
        return gridOptions;
    };
    
    $scope.validateGridData = function () {
        if ($scope.gridApi) {
            angular.forEach(blade.currentEntities, function (rowEntity) {
                angular.forEach($scope.gridOptions.columnDefs, function (colDef) {
                    $scope.gridApi.grid.validate.runValidators(rowEntity, colDef, rowEntity[colDef.name], undefined, $scope.gridApi.grid)
                });
            });
        }
    };

    blade.refresh();
}])
.factory('virtoCommerce.catalogModule.ingredientValidatorsService', [function () {
    var allIngredients = {};
    return {
        setAllIngredients: function (data) {
            allIngredients = data;
        },
        isMeasureValid: function (data) {
            //return data.list >= 0;
            return true;
        },
        isAmmountValid: function (data) {
            //return _.isUndefined(data.sale) || data.list >= data.sale;
            return true;
        },
        isUniqueSortOrder: function (data) {
            //return Math.round(data.minQuantity) > 0 && _.all(allPrices, function (x) { return x === data || Math.round(x.minQuantity) !== Math.round(data.minQuantity) });
            return true;
        },
        isUniqueQtyForPricelist: function (data) {
            //return _.filter(allPrices, function (price) { return price.pricelistId == data.pricelistId && price.minQuantity == data.minQuantity }).length == 1;
            return true;
        }
    };
}])
;
