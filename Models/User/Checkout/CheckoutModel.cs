using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace EAD_BE.Models.User.Checkout
{
    public class CheckoutItem
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class CheckoutModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public Guid CheckoutUuid { get; set; } = Guid.NewGuid();
        public string UserEmail { get; set; }
        public List<CheckoutItem> Items { get; set; } = new List<CheckoutItem>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // properties for purchase and payment status
        public string PurchaseStatus { get; set; } = "Pending"; // Default status
        public string PaymentStatus { get; set; } = "Pending"; // Default status
    }
}