using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApiProject.BLL;
using WebApiProject.BLL.Interfaces;
using WebApiProject.DAL;
using WebApiProject.DAL.Interfaces;
using WebApiProject.Data;
using WebApiProject.Models;
using WebApiProject.Models.DTO;
using Xunit;

namespace WebApiProject.Tests
{
    public class InventoryTests
    {
        private AppDbContext CreateInMemoryDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new AppDbContext(options);
        }

        private IShoppingDAL CreateShoppingDal(AppDbContext db)
        {
            return new ShoppingDAL(db);
        }

        private IShoppingBLLService CreateShoppingBll(AppDbContext db)
        {
            var shoppingDal = new ShoppingDAL(db);
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Shopping, ShoppingDTO>().ReverseMap();
            });
            var mapper = mapperConfig.CreateMapper();

            // simple stub for IGiftBLLService
            var giftBll = new TestGiftBLL();
            var logger = new NullLogger<ShoppingBLLService>();
            return new ShoppingBLLService(shoppingDal, mapper, giftBll, logger, db);
        }

        [Fact]
        public async Task ConfirmShopping_DecrementsInventory_WhenEnoughQuantity()
        {
            var db = CreateInMemoryDb("ConfirmShopping_Success");

            // seed gift, inventory, user, shopping
            var donor = new Donor { Id = 1, Name = "D1", Email = "d1@example.com", Phone = "0500000001" };
            db.Donors.Add(donor);
            var gift = new Gift { Id = 1, Name = "TestGift", Category = "General", CardPrice = 10, DonorId = donor.Id, Donor = donor, IsRaffled = false };
            db.Gifts.Add(gift);
            db.Inventories.Add(new Inventory { GiftId = 1, Quantity = 10 });
            db.Users.Add(new User { Id = 1, UserName = "u1", Email = "u1@example.com", Role = WebApiProject.RoleEnum.User, Phone = "010", PasswordHash = "hash" });
            db.Shoppings.Add(new Shopping { Id = 1, UserId = 1, GiftId = 1, Quantity = 3, IsDraft = true });
            await db.SaveChangesAsync();

            var bll = CreateShoppingBll(db);

            var result = await bll.ConfirmShopping(1);

            Assert.True(result);

            var inv = await db.Inventories.FirstAsync(i => i.GiftId == 1);
            Assert.Equal(7, inv.Quantity);

            var shopping = await db.Shoppings.FirstAsync(s => s.Id == 1);
            Assert.False(shopping.IsDraft);
        }

        [Fact]
        public async Task ConfirmShopping_Fails_WhenNotEnoughInventory()
        {
            var db = CreateInMemoryDb("ConfirmShopping_Fail");

            var donor2 = new Donor { Id = 2, Name = "D2", Email = "d2@example.com", Phone = "0500000002" };
            db.Donors.Add(donor2);
            db.Gifts.Add(new Gift { Id = 2, Name = "G2", Category = "General", CardPrice = 5, DonorId = donor2.Id, Donor = donor2, IsRaffled = false });
            db.Inventories.Add(new Inventory { GiftId = 2, Quantity = 1 });
            db.Users.Add(new User { Id = 2, UserName = "u2", Email = "u2@example.com", Role = WebApiProject.RoleEnum.User, Phone = "011", PasswordHash = "hash" });
            db.Shoppings.Add(new Shopping { Id = 2, UserId = 2, GiftId = 2, Quantity = 3, IsDraft = true });
            await db.SaveChangesAsync();

            var bll = CreateShoppingBll(db);

            await Assert.ThrowsAsync<Exception>(async () => await bll.ConfirmShopping(2));

            var inv = await db.Inventories.FirstAsync(i => i.GiftId == 2);
            Assert.Equal(1, inv.Quantity); // unchanged

            var shopping = await db.Shoppings.FirstAsync(s => s.Id == 2);
            Assert.True(shopping.IsDraft);
        }

        // Minimal stub for IGiftBLLService to satisfy constructor
        class TestGiftBLL : IGiftBLLService
        {
            public Task Add(GiftDTO giftDTO) => Task.CompletedTask;
            public Task<List<GiftGetDTO>> Get() => Task.FromResult(new List<GiftGetDTO>());
            public Task<List<GiftGetDTO>> GetFiltered(GiftFilterDTO filter) => Task.FromResult(new List<GiftGetDTO>());
            public Task<GiftGetDTO?> GetById(int id) => Task.FromResult<GiftGetDTO?>(null);
            public Task<bool> Delete(int id) => Task.FromResult(false);
            public Task<bool> Put(int id, GiftDTO giftDTO) => Task.FromResult(false);
            public Task<int> GetTotalIncome() => Task.FromResult(0);
            public Task<RaffleResultDTO?> RaffleGift(int giftId) => Task.FromResult<RaffleResultDTO?>(null);
            public Task<List<RaffleResultDTO>> RaffleAll() => Task.FromResult(new List<RaffleResultDTO>());
        }
    }
}
