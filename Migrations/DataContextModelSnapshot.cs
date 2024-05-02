﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Survey_Backend.Data;

#nullable disable

namespace Survey_Backend.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Survey_Backend.Models.Department", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("DepartmentName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProjectName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Departments");
                });

            modelBuilder.Entity("Survey_Backend.Models.Dimension", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("DimensionName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProjectName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Dimensions");
                });

            modelBuilder.Entity("Survey_Backend.Models.Generation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("GenerationName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("YearRangeLower")
                        .HasColumnType("int");

                    b.Property<int>("YearRangeUpper")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Generations");
                });

            modelBuilder.Entity("Survey_Backend.Models.Question", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Index")
                        .HasColumnType("int");

                    b.Property<string>("QuestionDescription")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("QuestionGroupId")
                        .HasColumnType("int");

                    b.Property<string>("QuestionGroupName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("QuestionText")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("SurveySetId")
                        .HasColumnType("int");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Questions");
                });

            modelBuilder.Entity("Survey_Backend.Models.QuestionResponse", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ResponderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("AnswerScore")
                        .HasColumnType("int");

                    b.Property<string>("AnswerText")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("ResponseDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id", "ResponderId");

                    b.ToTable("QuestionResponses");
                });

            modelBuilder.Entity("Survey_Backend.Models.ResponderInfo", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("SurveySetId")
                        .HasColumnType("int");

                    b.Property<int>("DepartmentId")
                        .HasColumnType("int");

                    b.Property<string>("DepartmentName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("GenerationId")
                        .HasColumnType("int");

                    b.Property<string>("IPAddress")
                        .HasColumnType("nvarchar(45)");

                    b.Property<int>("RoleId")
                        .HasColumnType("int");

                    b.Property<string>("RoleName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("SurveyCompletedTime")
                        .HasColumnType("datetime2");

                    b.HasKey("Id", "SurveySetId");

                    b.ToTable("ResponderInfos");
                });

            modelBuilder.Entity("Survey_Backend.Models.Role", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ProjectName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RoleName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("Survey_Backend.Models.Survey", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ProjectName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("SurveyCreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("SurveyDescription")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Surveys");
                });

            modelBuilder.Entity("Survey_Backend.Models.SurveyQuestions", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<int>("QuestionIndex")
                        .HasColumnType("int");

                    b.Property<Guid>("QuestionId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id", "QuestionIndex");

                    b.ToTable("SurveyQuestions");
                });
#pragma warning restore 612, 618
        }
    }
}
