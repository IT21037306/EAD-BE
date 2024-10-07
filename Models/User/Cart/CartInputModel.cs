/*
 * File: CartInputModel.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Model class of User Operations for Cart Management
 */


namespace EAD_BE.Models.User.Cart;

public class CartItemInput
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}