﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Project_Ecomm_1130.DataAccess.Repository.IRepository;
using Project_Ecomm_1130.Models;
using Project_Ecomm_1130.Models.ViewModels;
using Project_Ecomm_1130.Uitlity;
using Stripe;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;

namespace Project_Ecomm_1130.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private static bool isEmailConfirm = false;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;
        public CartController(IUnitOfWork unitOfWork,
            IEmailSender emailSender,
            UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _userManager = userManager;
        }
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
            {
                ShoppingCartVM = new ShoppingCartVM()
                {
                    ListCart = new List<ShoppingCart>()
                };
                return View(ShoppingCartVM);
            }
            //***
            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCart.GetAll
                (sc => sc.ApplicationUserId == claim.Value, includeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };
            ShoppingCartVM.OrderHeader.OrderTotal = 0;
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.
                FirstOrDefault(u => u.Id == claim.Value);
            foreach (var list in ShoppingCartVM.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(list.Count, list.Product.Price,
                    list.Product.Price50, list.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);
                if (list.Product.Description.Length > 100)
                {
                    list.Product.Description = list.Product.Description.Substring(0, 99) + "...";
                }
            }
            //*******
            if (!isEmailConfirm)
            {
                ViewBag.EmailMessage = "Email has been sent kindly verify your email !";
                ViewBag.EmailCSS = "text-success";
                isEmailConfirm = false;
            }
            else
            {
                ViewBag.EmailMessage = "Email Must be confirm for authorize customer !";
                ViewBag.EmailCSS = "text-danger";
            }

            return View(ShoppingCartVM);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPost()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = _unitOfWork.ApplicationUser.FirstOrDefault(u=>u.Id==claims.Value);
            if(user==null)
            {
                ModelState.AddModelError(string.Empty, "Email is Empty");
            }
            else
            {
                //Email
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                // *****
                isEmailConfirm = true;
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult plus(int id)
        {
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(sc => sc.Id == id);
            cart.Count += 1;
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult minus(int id)
        {
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(sc => sc.Id == id);
            if (cart.Count == 1)
                cart.Count = 1;
            else
                cart.Count -= 1;
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult delete(int id)
        {   
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(sc => sc.Id == id);
            _unitOfWork.ShoppingCart.Remove(cart);
            _unitOfWork.Save();
            //Session
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
            {
                var count = _unitOfWork.ShoppingCart.GetAll
                    (sc=>sc.ApplicationUserId==claim.Value).ToList().Count;
                HttpContext.Session.SetInt32(SD.Ss_CartSessionCount,count);
            }
            return RedirectToAction(nameof(Index));            
        }
        public IActionResult Summary()
        {
            var claimsIdentity=(ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCart.GetAll
                (sc => sc.ApplicationUserId == claim.Value, includeProperties: "Product"),
                OrderHeader = new OrderHeader() 
            };
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.
                FirstOrDefault(u=>u.Id==claim.Value);

            foreach (var list in ShoppingCartVM.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(list.Count,list.Product.Price,
                    list.Product.Price50,list.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (list.Count * list.Price);
                list.Product.Description=SD.ConvertToRawHtml(list.Product.Description);
            }
            ShoppingCartVM.OrderHeader.Name=ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.StreetAddress=ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.State=ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.City=ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.PostalCode=ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            return View(ShoppingCartVM);
        }
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [ActionName("Summary")]
        public IActionResult SummaryPost(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.
               ApplicationUser.FirstOrDefault(u => u.Id == claim.Value);

            ShoppingCartVM.ListCart = _unitOfWork.ShoppingCart.GetAll
                (sc => sc.ApplicationUserId == claim.Value,
                includeProperties: "Product");
            ShoppingCartVM.OrderHeader.Carrier = "";
            ShoppingCartVM.OrderHeader.TrackingNumber = "";
            ShoppingCartVM.OrderHeader.TransactionId = "";

            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartVM.OrderHeader.OrderStatus = SD.OrderStatusPending;
            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            foreach (var list in ShoppingCartVM.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(list.Count,list.Product.Price,
                    list.Product.Price50,list.Product.Price100);
                OrderDetail orderDetail = new OrderDetail()
                {
                    ProductId=list.ProductId,
                    OrderHeaderId=ShoppingCartVM.OrderHeader.Id,
                    Price=list.Price,
                    Count=list.Count
                };
                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }
            _unitOfWork.ShoppingCart.RemoveRange(ShoppingCartVM.ListCart);
            _unitOfWork.Save();
            //Session
            HttpContext.Session.SetInt32(SD.Ss_CartSessionCount,0);
            #region Stripe Process
            if(stripeToken==null)
            {
                ShoppingCartVM.OrderHeader.PaymentDueDate = DateTime.Today.AddDays(30);
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.OrderStatusApproved;
            }
            else
            {
                //Payment Process
                var options = new ChargeCreateOptions()
                {
                    Amount = Convert.ToInt32(ShoppingCartVM.OrderHeader.OrderTotal),
                    Currency = "usd",
                    Description = "Order Id : " + ShoppingCartVM.OrderHeader.Id,
                    Source=stripeToken
                };
                //Payment
                var service =new ChargeService();
                Charge charge = service.Create(options);
                if (charge.BalanceTransactionId == null)
                    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
                else 
                    ShoppingCartVM.OrderHeader.TransactionId= charge.BalanceTransactionId;

                if(charge.Status.ToLower()== "succeeded")
                {
                    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                    ShoppingCartVM.OrderHeader.OrderStatus = SD.OrderStatusApproved;
                    ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
                }
                _unitOfWork.Save();
            }
            #endregion
            return RedirectToAction("OrderConfirmation","Cart",
                new {id=ShoppingCartVM.OrderHeader.Id });
        }
        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }
    }
}
