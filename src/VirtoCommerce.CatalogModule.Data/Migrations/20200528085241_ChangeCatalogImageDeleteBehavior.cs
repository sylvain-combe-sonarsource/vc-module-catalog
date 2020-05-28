using Microsoft.EntityFrameworkCore.Migrations;

namespace VirtoCommerce.CatalogModule.Data.Migrations
{
    public partial class ChangeCatalogImageDeleteBehavior : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatalogImage_Category_CategoryId",
                table: "CatalogImage");

            migrationBuilder.DropForeignKey(
                name: "FK_CatalogImage_Item_ItemId",
                table: "CatalogImage");

            migrationBuilder.AddForeignKey(
                name: "FK_CatalogImage_Category_CategoryId",
                table: "CatalogImage",
                column: "CategoryId",
                principalTable: "Category",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CatalogImage_Item_ItemId",
                table: "CatalogImage",
                column: "ItemId",
                principalTable: "Item",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatalogImage_Category_CategoryId",
                table: "CatalogImage");

            migrationBuilder.DropForeignKey(
                name: "FK_CatalogImage_Item_ItemId",
                table: "CatalogImage");

            migrationBuilder.AddForeignKey(
                name: "FK_CatalogImage_Category_CategoryId",
                table: "CatalogImage",
                column: "CategoryId",
                principalTable: "Category",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CatalogImage_Item_ItemId",
                table: "CatalogImage",
                column: "ItemId",
                principalTable: "Item",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
