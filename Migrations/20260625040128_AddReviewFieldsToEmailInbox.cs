using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailClassifier.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewFieldsToEmailInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrectedCategory",
                table: "EmailInboxes",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "EmailInboxes",
                type: "boolean",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsReviewed",
                table: "EmailInboxes",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "EmailInboxes",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "EmailInboxes",
                type: "timestamp with time zone",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CorrectedCategory", table: "EmailInboxes");

            migrationBuilder.DropColumn(name: "IsCorrect", table: "EmailInboxes");

            migrationBuilder.DropColumn(name: "IsReviewed", table: "EmailInboxes");

            migrationBuilder.DropColumn(name: "ReviewNote", table: "EmailInboxes");

            migrationBuilder.DropColumn(name: "ReviewedAt", table: "EmailInboxes");
        }
    }
}
