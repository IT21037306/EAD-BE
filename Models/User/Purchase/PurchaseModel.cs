/*
 * File: PurchaseModel.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Model class of User Operations for Purchased Items
 */


using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EAD_BE.Models.User.Purchased;

public class PurchaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public Guid PurchaseId { get; set; }
    public string userEmail { get; set; }
    public DateTime PurchaseDate { get; set; }
    public List<PurchasedItem> Items { get; set; }
    public bool IsShipped { get; set; } = false;
    public bool IsDelivered { get; set; } = false;
    
    public bool IsUserDataAvailable { get; set; } = false;
    
    public UserData UserDetails { get; set; }
    
    public bool isOrderCancelled { get; set; } = false;
    
    public bool requestToCancelOrder { get; set; } = false;
    
}

public class PurchasedItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class UserData
{
    public string UserName { get; set; }
    public string UserPhoneNumber { get; set; }
    public string UserAddress { get; set; }
}