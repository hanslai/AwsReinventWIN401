﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CartService.Model;
using CartService.Session;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProductService.Model;

namespace CartService.Controllers
{
    [Produces("application/json")]
    [Route("api/Cart")]
    [EnableCors("AllowAllOrigins")]
    public class CartController : Controller
    {
        private const string CartKey = "Cart";
        private Cart _cart;
        private bool _dev = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        private void LoadCart()
        {
            HttpContext.Session.LoadAsync().Wait();

            if (HttpContext.Session.IsAvailable && HttpContext.Session.Keys.Contains(CartKey))
            {
                _cart = HttpContext.Session.GetObject<Cart>(CartKey);
            }
            else
            {
                _cart = new Cart();
                HttpContext.Session.SetObject(CartKey, _cart);
            }
        }
        
        // GET: api/Cart
        [HttpGet]
        public IActionResult Get()
        {
            LoadCart();

            //TEMP test code - remove later!!
            if (!_cart.Items.Any())
                AddDummyCartItems();

            return Ok(_cart.ItemsCollection());
        }

        // POST: api/Cart
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]CartItem item)
        {
            LoadCart();

            item.DateAdded = DateTime.Now;

            //check if quantity is available
            var product = await GetProductFromProductServiceAsync(item.ProductId);
            if (product != null && item.Quantity > product.AvailableStock)
            {
                return BadRequest($"Insuffient quantity in stock: {product.AvailableStock} in stock, attempted to add {item.Quantity}");
            }

            _cart.Add(item);

            HttpContext.Session.SetObject(CartKey, _cart);

            return Ok(item.ProductId);
        }

        // PUT: api/Cart/01234-5678-9abcd-efg
        [HttpPut("{id}")]
        public IActionResult Put(Guid id, [FromBody]CartItem item)
        {
            LoadCart();

            if (id != item.ProductId)
                return BadRequest();

            if (!_cart.Contains(id))
                return NotFound();

            var existingItem = _cart.Items[id];

            existingItem.Quantity = item.Quantity;
            existingItem.PriceWhenAdded = item.PriceWhenAdded;
            HttpContext.Session.SetObject(CartKey, _cart);

            return Ok();
        }
        
        // DELETE: api/ApiWithActions/01234-5678-9abcd-efg
        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            LoadCart();

            if (!_cart.Contains(id))
                return NotFound();

            _cart.Remove(id);
            HttpContext.Session.SetObject(CartKey, _cart);

            return Ok();
        }

        // GET: api/cart/health
        [HttpGet]
        [Route("/api/cart/health")]
        public IActionResult Health()
        {
            return Ok();
        }

        private async Task<Product> GetProductFromProductServiceAsync(Guid productId)
        {
            if (_dev) return null;  //for running locally

            var http = new HttpClient
            {
                BaseAddress = new Uri("http://product.techsummit/api/")
            };

            try
            {
                var prodString =  await http.GetStringAsync("products");
                var product = JsonConvert.DeserializeObject<Product>(prodString);

                return product;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        private void AddDummyCartItems()
        {
            LoadCart();

            _cart.Add(new CartItem
            {
                ProductId = Guid.Parse("dfc55ec1-2f35-4d11-9543-a52622ef00d5"),
                Name = ".NET Bot Black Hoodie",
                Quantity = 2,
                PriceWhenAdded = 23.99M,
                DateAdded = DateTime.Now
            });
            _cart.Add(new CartItem
            {
                ProductId = Guid.Parse("a65bddcf-14b9-4990-a15e-6a9afa9679c6"),
                Name = ".NET Black & White Mug",
                Quantity = 1,
                PriceWhenAdded = 19.95M,
                DateAdded = DateTime.Now
            });

            HttpContext.Session.SetObject(CartKey, _cart);
        }
    }
}
