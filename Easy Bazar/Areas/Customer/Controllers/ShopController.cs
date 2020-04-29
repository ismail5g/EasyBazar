﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataSets.Interfaces;
using DataSets.Utility;
using DataSets.Entity;
using DataSets.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Easy_Bazar.Code;

namespace Easy_Bazar.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ShopController : Controller
    {
        private readonly IUnitOfWork _uow;
        public ShopController(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public IActionResult Index(string searchterm, int? Sortby, int? minPrice, int? maxPrice, int? cateId)
        {
            var model = new HomeVM();
            model.Products = _uow.Product.GetAll().ToList();
            model.Categories = _uow.Category.GetAll().ToList();

            if (cateId.HasValue)
            {
                model.Products = _uow.Product.GetAll().Where(x=>x.CategoryID==cateId.Value).ToList();
            }
            if (!string.IsNullOrEmpty(searchterm))
            {
                model.Products = _uow.Product.GetAll().Where(x=>x.Name.ToLower().Contains(searchterm.ToLower())).ToList();
            }
            if (minPrice.HasValue)
            {
                model.Products = _uow.Product.GetAll().Where(x=>x.Price>=minPrice.Value).ToList();
            }
            if (maxPrice.HasValue)
            {
                model.Products = _uow.Product.GetAll().Where(x => x.Price <= maxPrice.Value).ToList();
            }

            if (Sortby.HasValue)
            {
                var sort = (SortByEnums)Sortby.Value;
                model.sortBy = Sortby;
                switch (sort)
                {
                    case SortByEnums.Default:
                        model.Products = _uow.Product.GetAll().OrderByDescending(x=>x.ID).ToList();
                        break;
                    case SortByEnums.Popularity:
                        model.Products = _uow.Product.GetAll().ToList();
                        //Popularity Need to set
                        break;
                    case SortByEnums.PricelowToHigh:
                        model.Products = _uow.Product.GetAll().OrderBy(x=>x.Price).ToList();
                        break;
                    case SortByEnums.PriceHighToLow:
                        model.Products = _uow.Product.GetAll().OrderByDescending(x=>x.Price).ToList();
                        break;
                }
            }
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
            {
                var shoppingCount = _uow.ShoppingCart.GetAll(a => a.ApplicationUserId == claim.Value).ToList().Count();

                HttpContext.Session.SetInt32(ProjectConstant.shoppingCart, shoppingCount);
            }
            return View(model);
        }


        public IActionResult Details(int id)
        {
            var product = _uow.Product.GetFirstOrDefault(p => p.ID == id, includeProperties: "Category");

            ShoppingCart cart = new ShoppingCart()
            {
                Product = product,
                ProductId = product.ID
            };

            var listobj = _uow.Category.GetAll().ToList();
            var ok = listobj.FirstOrDefault(x => x.ID == product.CategoryID).Name;
            TempData["CatName"] = ok;
            return View(cart);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart cartObj)
        {
            cartObj.Id = 0;
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                cartObj.ApplicationUserId = claim.Value;

                ShoppingCart fromDb = _uow.ShoppingCart.GetFirstOrDefault(
                    s => s.ApplicationUserId == cartObj.ApplicationUserId
                    && s.ProductId == cartObj.ProductId,
                    includeProperties: "Product");

                if (fromDb == null)
                {
                    //Insert
                    _uow.ShoppingCart.Add(cartObj);
                }
                else
                {
                    //Update
                    fromDb.Count += cartObj.Count;
                }

                _uow.Save();

                var shoppingCount = _uow.ShoppingCart.GetAll(a => a.ApplicationUserId == cartObj.ApplicationUserId).ToList().Count();

                HttpContext.Session.SetInt32(ProjectConstant.shoppingCart, shoppingCount);

                return RedirectToAction(nameof(Index));
            }
            else
            {
                var product = _uow.Product.GetFirstOrDefault(p => p.ID == cartObj.ProductId, includeProperties: "Category");

                ShoppingCart cart = new ShoppingCart()
                {
                    Product = product,
                    ProductId = product.ID
                };
                return View(cart);
            }
        }
    }
}