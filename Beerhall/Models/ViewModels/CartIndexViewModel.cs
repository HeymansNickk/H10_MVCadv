using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beerhall.Models.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Beerhall.Models.ViewModels
{
    public class CartIndexViewModel
    {
        [HiddenInput]
        public int BeerId { get; }
        public int Quantity { get; }
        public string Beer { get; }
        public decimal Price { get; }
        public decimal SubTotal { get; }

        public CartIndexViewModel(CartLine cartLine)
        {
            BeerId = cartLine.Product.BeerId;
            Quantity = cartLine.Quantity;
            Beer = cartLine.Product.Name;
            Price = cartLine.Product.Price;
            SubTotal = cartLine.Total;
        }
    }
}
