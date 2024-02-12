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
    [Migration("20240202114651_AddDuplicateProspectsTable")]
    partial class AddDuplicateProspectsTable
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

            modelBuilder.Entity("Capitalead.Data.ProcessedRun", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

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

                    b.HasKey("Id");

                    b.HasIndex("Phone")
                        .IsUnique();

                    b.ToTable("Prospects");
                });
#pragma warning restore 612, 618
        }
    }
}
