using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIWaveRelease.Migrations
{
    /// <inheritdoc />
    public partial class migracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WaveRelease",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodMastr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodInr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CantMastr = table.Column<int>(type: "int", nullable: false),
                    CantInr = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Familia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumOrden = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodProducto = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Wave = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaveRelease", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WaveRelease");
        }
    }
}
