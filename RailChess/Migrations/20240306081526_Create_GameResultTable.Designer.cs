﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RailChess.Models.DbCtx;

#nullable disable

namespace RailChess.Migrations
{
    [DbContext(typeof(RailChessContext))]
    [Migration("20240306081526_Create_GameResultTable")]
    partial class Create_GameResultTable
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.15");

            modelBuilder.Entity("RailChess.Models.Game.GameResult", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("EloDelta")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GameId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Rank")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Score")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("GameResults");
                });

            modelBuilder.Entity("RailChess.Models.Game.RailChessEvent", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("EventType")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GameId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PlayerId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("StationId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Time")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Events");
                });

            modelBuilder.Entity("RailChess.Models.Game.RailChessGame", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AllowReverseAtTerminal")
                        .HasColumnType("INTEGER");

                    b.Property<int>("AllowTransfer")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Deleted")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DurationMins")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Ended")
                        .HasColumnType("INTEGER");

                    b.Property<int>("HostUserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RandAlg")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RandMax")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RandMin")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Started")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Steps")
                        .HasColumnType("INTEGER");

                    b.Property<int>("StucksToLose")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UseMapId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Games");
                });

            modelBuilder.Entity("RailChess.Models.Map.RailChessMap", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Author")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Deleted")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExcStationCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ImgFileName")
                        .HasColumnType("TEXT");

                    b.Property<int>("LineCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("StationCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.Property<string>("TopoData")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("UpdateTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Maps");
                });

            modelBuilder.Entity("RailChess.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AvatarName")
                        .HasColumnType("TEXT");

                    b.Property<int>("Elo")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Pwd")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
