using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLiKhachSan.Migrations
{
    public partial class AddBookingStatusHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    FromBookingStatusId = table.Column<int>(type: "int", nullable: true),
                    ToBookingStatusId = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingStatusHistories_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingStatusHistories_BookingStatuses_FromBookingStatusId",
                        column: x => x.FromBookingStatusId,
                        principalTable: "BookingStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingStatusHistories_BookingStatuses_ToBookingStatusId",
                        column: x => x.ToBookingStatusId,
                        principalTable: "BookingStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingStatusHistories_BookingId",
                table: "BookingStatusHistories",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingStatusHistories_FromBookingStatusId",
                table: "BookingStatusHistories",
                column: "FromBookingStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingStatusHistories_ToBookingStatusId",
                table: "BookingStatusHistories",
                column: "ToBookingStatusId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingStatusHistories");
        }
    }
}
