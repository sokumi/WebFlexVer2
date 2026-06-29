using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebFlex.UI.Migrations
{
    /// <inheritdoc />
    public partial class model_004 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "opc_tag",
                type: "integer",
                nullable: true,
                comment: "정렬 순서",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "정렬 순서")
                .Annotation("Relational:ColumnOrder", 34)
                .OldAnnotation("Relational:ColumnOrder", 23);

            migrationBuilder.AlterColumn<bool>(
                name: "show_on_dashboard",
                table: "opc_tag",
                type: "boolean",
                nullable: false,
                comment: "대시보드 표시 여부",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "대시보드 표시 여부")
                .Annotation("Relational:ColumnOrder", 32)
                .OldAnnotation("Relational:ColumnOrder", 20);

            migrationBuilder.AlterColumn<bool>(
                name: "save_to_database",
                table: "opc_tag",
                type: "boolean",
                nullable: false,
                comment: "DB 저장 여부",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "DB 저장 여부")
                .Annotation("Relational:ColumnOrder", 31)
                .OldAnnotation("Relational:ColumnOrder", 19);

            migrationBuilder.AlterColumn<int>(
                name: "samplingintervalms",
                table: "opc_tag",
                type: "integer",
                nullable: true,
                comment: "Sampling Interval(ms)",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "Sampling Interval(ms)")
                .Annotation("Relational:ColumnOrder", 33)
                .OldAnnotation("Relational:ColumnOrder", 21);

            migrationBuilder.AlterColumn<bool>(
                name: "is_collectenabled",
                table: "opc_tag",
                type: "boolean",
                nullable: false,
                comment: "수집 사용 여부",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "수집 사용 여부")
                .Annotation("Relational:ColumnOrder", 30)
                .OldAnnotation("Relational:ColumnOrder", 18);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "opc_tag",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "설명",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "설명")
                .Annotation("Relational:ColumnOrder", 35)
                .OldAnnotation("Relational:ColumnOrder", 24);

            migrationBuilder.AddColumn<string>(
                name: "protect_type",
                table: "opc_tag",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                comment: "권한 타입")
                .Annotation("Relational:ColumnOrder", 18);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "protect_type",
                table: "opc_tag");

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "opc_tag",
                type: "integer",
                nullable: true,
                comment: "정렬 순서",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "정렬 순서")
                .Annotation("Relational:ColumnOrder", 23)
                .OldAnnotation("Relational:ColumnOrder", 34);

            migrationBuilder.AlterColumn<bool>(
                name: "show_on_dashboard",
                table: "opc_tag",
                type: "boolean",
                nullable: false,
                comment: "대시보드 표시 여부",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "대시보드 표시 여부")
                .Annotation("Relational:ColumnOrder", 20)
                .OldAnnotation("Relational:ColumnOrder", 32);

            migrationBuilder.AlterColumn<bool>(
                name: "save_to_database",
                table: "opc_tag",
                type: "boolean",
                nullable: false,
                comment: "DB 저장 여부",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "DB 저장 여부")
                .Annotation("Relational:ColumnOrder", 19)
                .OldAnnotation("Relational:ColumnOrder", 31);

            migrationBuilder.AlterColumn<int>(
                name: "samplingintervalms",
                table: "opc_tag",
                type: "integer",
                nullable: true,
                comment: "Sampling Interval(ms)",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "Sampling Interval(ms)")
                .Annotation("Relational:ColumnOrder", 21)
                .OldAnnotation("Relational:ColumnOrder", 33);

            migrationBuilder.AlterColumn<bool>(
                name: "is_collectenabled",
                table: "opc_tag",
                type: "boolean",
                nullable: false,
                comment: "수집 사용 여부",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "수집 사용 여부")
                .Annotation("Relational:ColumnOrder", 18)
                .OldAnnotation("Relational:ColumnOrder", 30);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "opc_tag",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "설명",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "설명")
                .Annotation("Relational:ColumnOrder", 24)
                .OldAnnotation("Relational:ColumnOrder", 35);
        }
    }
}
