﻿// <auto-generated />
using System;
using HierarchyComposer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Mop.Hierarchy.Migrations
{
    [DbContext(typeof(HierarchyContext))]
    partial class HierarchyContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

            modelBuilder.Entity("HierarchyComposer.Model.Node", b =>
                {
                    b.Property<uint>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<uint?>("AABBId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("DiagnosticInfo")
                        .HasColumnType("TEXT");

                    b.Property<uint>("EndId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("HasMesh")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<uint?>("ParentId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("RefNoDb")
                        .HasColumnType("INTEGER");

                    b.Property<string>("RefNoPrefix")
                        .HasColumnType("TEXT");

                    b.Property<int?>("RefNoSequence")
                        .HasColumnType("INTEGER");

                    b.Property<uint>("TopNodeId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AABBId");

                    b.HasIndex("ParentId");

                    b.HasIndex("TopNodeId");

                    b.ToTable("Nodes", (string)null);
                });

            modelBuilder.Entity("HierarchyComposer.Model.NodePDMSEntry", b =>
                {
                    b.Property<uint>("NodeId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("PDMSEntryId")
                        .HasColumnType("INTEGER");

                    b.HasKey("NodeId", "PDMSEntryId");

                    b.HasIndex("PDMSEntryId");

                    b.ToTable("NodeToPDMSEntry");
                });

            modelBuilder.Entity("HierarchyComposer.Model.PDMSEntry", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Key")
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("PDMSEntries", (string)null);
                });

            modelBuilder.Entity("HierarchyComposer.Model.NodePDMSEntry", b =>
                {
                    b.HasOne("HierarchyComposer.Model.Node", "Node")
                        .WithMany("NodePDMSEntry")
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("HierarchyComposer.Model.PDMSEntry", "PDMSEntry")
                        .WithMany("NodePDMSEntry")
                        .HasForeignKey("PDMSEntryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Node");

                    b.Navigation("PDMSEntry");
                });

            modelBuilder.Entity("HierarchyComposer.Model.Node", b =>
                {
                    b.Navigation("NodePDMSEntry");
                });

            modelBuilder.Entity("HierarchyComposer.Model.PDMSEntry", b =>
                {
                    b.Navigation("NodePDMSEntry");
                });
#pragma warning restore 612, 618
        }
    }
}
