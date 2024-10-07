/*
 * File: CartModel.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Model class of User Operations for Cart
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace EAD_BE.Models.User.Cart
{
    public class CartItem
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class Cart
    {
        [BsonId] // Maps this property to MongoDB's _id field
        [BsonRepresentation(BsonType.ObjectId)] // Stored as ObjectId in MongoDB
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString(); // Initialize with a new ObjectId

        public Guid CartUuid { get; set; } = Guid.NewGuid(); 
        public String UserEmail { get; set; }
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}