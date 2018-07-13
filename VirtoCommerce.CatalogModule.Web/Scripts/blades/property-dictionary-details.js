angular.module('virtoCommerce.catalogModule')
    .controller('virtoCommerce.catalogModule.propertyDictionaryDetailsController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.catalogModule.properties',
        function ($scope, bladeNavigationService, properties) {
            var blade = $scope.blade;
            blade.headIcon = 'fa-book';
            $scope.blade = blade;

            blade.refresh = function (parentRefresh) {
                blade.isLoading = false;

                // var searchCriteria = getSearchCriteria();

                // optionApi.search(searchCriteria, function (data) {
                //     blade.isLoading = false;
                //     blade.currentEntities = data.result;
                // });

                if (parentRefresh && blade.parentBlade.refresh) {
                    blade.parentBlade.refresh();
                }
            };

            blade.toolbarCommands = [
                {
                    name: "platform.commands.save", icon: 'fa fa-save',
                    executeMethod: saveChanges,
                    canExecuteMethod: canSave
                },
                {
                    name: "platform.commands.delete", icon: 'fa fa-trash-o',
                    executeMethod: function () {
                        removeProperty(blade.currentEntity);
                    },
                    canExecuteMethod: function () {
                        return blade.isManageable && !blade.isNew;
                    }
                }
            ];

            var formScope;
            $scope.setForm = function (form) { formScope = form; }

            function isDirty() {
                return !angular.equals(blade.property, blade.currentEntity) && blade.hasUpdatePermission();
            }
    
            function canSave() {
                return (blade.isNew || isDirty()) && formScope && formScope.$valid;
            }
    
            function saveChanges() {
                debugger;
                blade.isLoading = true;
    
                if (blade.currentEntity.valueType !== "ShortText" && blade.currentEntity.valueType !== "LongText") {
                    blade.currentEntity.validationRule = null;
                }
    
                properties.update(blade.currentEntity, function (data, headers) {
                    blade.currentEntityId = data.id;
                    blade.refresh(true);
                });
            };
    
            function removeProperty(prop) {
                debugger;
                var dialog = {
                    id: "confirmDelete",
                    messageValues: { name: prop.name },
                    callback: function (doDeleteValues) {
                        blade.isLoading = true;
    
                        properties.remove({ id: prop.id, doDeleteValues: doDeleteValues }, function () {
                            $scope.bladeClose();
                            blade.parentBlade.refresh();
                        });
                    }
                };
                dialogService.showDialog(dialog, 'Modules/$(VirtoCommerce.Catalog)/Scripts/dialogs/deleteProperty-dialog.tpl.html', 'platformWebApp.confirmDialogController');
            }
    
            blade.onClose = function (closeCallback) {
                bladeNavigationService.showConfirmationIfNeeded(isDirty(), canSave(), blade, saveChanges, closeCallback, "catalog.dialogs.property-save.title", "catalog.dialogs.property-save.message");
            };

            blade.refresh();
        }]);