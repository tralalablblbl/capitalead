﻿// <auto-generated />
using System;
using Capitalead.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Capitalead.Migrations
{
    [DbContext(typeof(AppDatabase))]
    [Migration("20240317151948_AddKpiColumns")]
    partial class AddKpiColumns
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Capitalead.Data.DuplicateProspect", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string[]>("Content")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<bool>("Deleted")
                        .HasColumnType("boolean");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("ProspectId")
                        .HasColumnType("bigint");

                    b.Property<long>("SheetId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("DuplicateProspects");
                });

            modelBuilder.Entity("Capitalead.Data.Import", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<long>("AddedCount")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("Completed")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Error")
                        .HasColumnType("text");

                    b.Property<DateTime>("Started")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Imports");
                });

            modelBuilder.Entity("Capitalead.Data.ProcessedRun", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ClusterId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("ProcessedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("ProspectsCount")
                        .HasColumnType("bigint");

                    b.Property<string>("RunId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("RunId")
                        .IsUnique();

                    b.ToTable("ProcessedRuns");
                });

            modelBuilder.Entity("Capitalead.Data.Prospect", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Energy")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("ImportId")
                        .HasColumnType("uuid");

                    b.Property<string>("Neighbourhood")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("ParsingDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("RealEstateType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Rooms")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Size")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long?>("SpreadsheetId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("ImportId");

                    b.HasIndex("Phone")
                        .IsUnique();

                    b.HasIndex("SpreadsheetId");

                    b.ToTable("Prospects");
                });

            modelBuilder.Entity("Capitalead.Data.Spreadsheet", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("ClusterId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ClusterName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("DisabledProspectsCount")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("LastParsingDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("LeadsCount")
                        .HasColumnType("bigint");

                    b.Property<long>("ProspectsCount")
                        .HasColumnType("bigint");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long?>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("Title")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("Spreadsheets");
                });

            modelBuilder.Entity("Capitalead.Data.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Firstname")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Lastname")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("MobilePhone")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Capitalead.Data.Prospect", b =>
                {
                    b.HasOne("Capitalead.Data.Import", "Import")
                        .WithMany("Prospects")
                        .HasForeignKey("ImportId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Capitalead.Data.Spreadsheet", "Spreadsheet")
                        .WithMany("Prospects")
                        .HasForeignKey("SpreadsheetId");

                    b.Navigation("Import");

                    b.Navigation("Spreadsheet");
                });

            modelBuilder.Entity("Capitalead.Data.Spreadsheet", b =>
                {
                    b.HasOne("Capitalead.Data.User", "User")
                        .WithMany("Spreadsheets")
                        .HasForeignKey("UserId");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Capitalead.Data.Import", b =>
                {
                    b.Navigation("Prospects");
                });

            modelBuilder.Entity("Capitalead.Data.Spreadsheet", b =>
                {
                    b.Navigation("Prospects");
                });

            modelBuilder.Entity("Capitalead.Data.User", b =>
                {
                    b.Navigation("Spreadsheets");
                });
#pragma warning restore 612, 618
        }
    }
}
