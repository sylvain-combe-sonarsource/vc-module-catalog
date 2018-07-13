angular.module('virtoCommerce.catalogModule')
    .controller('virtoCommerce.catalogModule.propertyDictionaryListController', ['$scope', 'platformWebApp.bladeNavigationService',
        function ($scope, bladeNavigationService) {
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

            $scope.groupedValues = _.map(_.groupBy(blade.property.dictionaryValues, 'alias'), function (values, key) {
                return { alias: key, values: values };
            });

            // Search Criteria
            function getSearchCriteria() {
                var searchCriteria = {
                    skip: 0,
                    take: imageToolsConfig.intMaxValue  // todo
                };
                return searchCriteria;
            }

            blade.toolbarCommands = [
                {
                    name: "platform.commands.refresh", icon: 'fa fa-refresh',
                    executeMethod: blade.refresh,
                    canExecuteMethod: function () {
                        return true;
                    }
                },
                {
                    name: "platform.commands.add", icon: 'fa fa-plus',
                    executeMethod: function () {
                        blade.setSelectedId(null);
                        showDetailBlade({ isNew: true });
                    },
                    canExecuteMethod: function () {
                        return true;
                    }
                }
            ];

            $scope.selectNode = function (listItem) {
                blade.setSelectedNode(listItem);
                showDetailBlade({ currentEntityId: listItem.id });
            };

            blade.setSelectedNode = function (setSelectedNode) {
                $scope.setSelectedNode = setSelectedNode;
            };

            function showDetailBlade(bladeData) {
                var newBlade = {
                    id: 'optionDetail',
                    title : 'catalog.blades.property-dictionary.labels.value-edit',
                    controller: 'virtoCommerce.catalogModule.propertyDictionaryDetailsController',
                    template: 'Modules/$(VirtoCommerce.Catalog)/Scripts/blades/property-dictionary-details.tpl.html',
                    currentEntity: blade.property.dictionaryValues.filter(x=>x.alias == $scope.setSelectedNode.alias),
                    multilanguage: blade.property.multilanguage,
                    alias: $scope.setSelectedNode.alias
                };
                angular.extend(newBlade, bladeData);
                bladeNavigationService.showBlade(newBlade, blade);
            };

            blade.refresh();
        }]);