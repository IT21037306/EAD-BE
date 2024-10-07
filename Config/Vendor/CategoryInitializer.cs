/*
 * File: CategoryInitializer.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Configuration class of Vendor Operations for Product Category Management
 */


using EAD_BE.Models.Vendor.Product;
using MongoDB.Driver;


namespace EAD_BE.Config.Vendor
{
    public class CategoryInitializer
    {
        private readonly IMongoCollection<CategoryModel> _categoryCollection;
        private readonly string[] _categories = { "Electronics", "Books", "Clothing", "Home & Kitchen", "Fruits", "Vegetables"};

        // Constructor
        public CategoryInitializer(IMongoCollection<CategoryModel> categoryCollection)
        {
            _categoryCollection = categoryCollection;
        }

        // Initialize Categories
        public async Task InitializeCategories()
        {
            foreach (var category in _categories)
            {
                var existingCategory = await _categoryCollection.Find(c => c.Name == category).FirstOrDefaultAsync();
                if (existingCategory == null)
                {
                    await _categoryCollection.InsertOneAsync(new CategoryModel() { Name = category , IsActive = true});
                }
            }
        }
    }
    
}