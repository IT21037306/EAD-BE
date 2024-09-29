namespace EAD_BE.Models.User.Cart;

public class CartItemInput
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}