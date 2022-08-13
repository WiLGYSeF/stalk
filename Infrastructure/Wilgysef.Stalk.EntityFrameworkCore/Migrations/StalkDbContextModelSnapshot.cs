﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Wilgysef.Stalk.EntityFrameworkCore;

#nullable disable

namespace Wilgysef.Stalk.EntityFrameworkCore.Migrations
{
    [DbContext(typeof(StalkDbContext))]
    partial class StalkDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.7");

            modelBuilder.Entity("Wilgysef.Stalk.Core.BackgroundJobs.BackgroundJob", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Abandoned")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Attempts")
                        .HasColumnType("INTEGER");

                    b.Property<string>("JobArgs")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("JobArgsName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("MaximumLifetime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("NextRun")
                        .HasColumnType("TEXT");

                    b.Property<int>("Priority")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("BackgroundJobs");
                });

            modelBuilder.Entity("Wilgysef.Stalk.Core.Models.Jobs.Job", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ConfigJson")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DelayedUntil")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("Finished")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<int>("Priority")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("Started")
                        .HasColumnType("TEXT");

                    b.Property<int>("State")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Jobs");
                });

            modelBuilder.Entity("Wilgysef.Stalk.Core.Models.JobTasks.JobTask", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("DelayedUntil")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("Finished")
                        .HasColumnType("TEXT");

                    b.Property<string>("ItemData")
                        .HasColumnType("TEXT");

                    b.Property<string>("ItemId")
                        .HasColumnType("TEXT");

                    b.Property<long?>("JobId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("MetadataJson")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<long?>("ParentTaskId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Priority")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("Started")
                        .HasColumnType("TEXT");

                    b.Property<int>("State")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Uri")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("JobId");

                    b.HasIndex("ParentTaskId");

                    b.ToTable("JobTasks");
                });

            modelBuilder.Entity("Wilgysef.Stalk.Core.Models.JobTasks.JobTask", b =>
                {
                    b.HasOne("Wilgysef.Stalk.Core.Models.Jobs.Job", null)
                        .WithMany("Tasks")
                        .HasForeignKey("JobId");

                    b.HasOne("Wilgysef.Stalk.Core.Models.JobTasks.JobTask", "ParentTask")
                        .WithMany()
                        .HasForeignKey("ParentTaskId");

                    b.OwnsOne("Wilgysef.Stalk.Core.Models.JobTasks.JobTaskResult", "Result", b1 =>
                        {
                            b1.Property<long>("JobTaskId")
                                .HasColumnType("INTEGER");

                            b1.Property<string>("ErrorCode")
                                .HasColumnType("TEXT");

                            b1.Property<string>("ErrorDetail")
                                .HasColumnType("TEXT");

                            b1.Property<string>("ErrorMessage")
                                .HasColumnType("TEXT");

                            b1.Property<bool?>("Success")
                                .HasColumnType("INTEGER");

                            b1.HasKey("JobTaskId");

                            b1.ToTable("JobTasks");

                            b1.WithOwner()
                                .HasForeignKey("JobTaskId");
                        });

                    b.Navigation("ParentTask");

                    b.Navigation("Result")
                        .IsRequired();
                });

            modelBuilder.Entity("Wilgysef.Stalk.Core.Models.Jobs.Job", b =>
                {
                    b.Navigation("Tasks");
                });
#pragma warning restore 612, 618
        }
    }
}
