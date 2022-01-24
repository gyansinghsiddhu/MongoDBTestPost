using MongoDBTest.Model;
using MongoDBTest.DAL;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;



namespace MongoDBTest
{
    class Program
    {

        private static IConfiguration _iconfiguration;
       

        static void Main(string[] args)
        {


            if (args.Length == 0)
            {
                args = new string[1];
                args[0] = "th";
                Scrapping("TH");
            }

            switch (args[0].ToString())
            {
                case "th":
                    Scrapping("TH");
                    break;
                case "id":
                    Scrapping("ID");
                    break;
                case "vi":
                    Scrapping("VN");
                    break;
                case "my":
                    Scrapping("MY");
                    break;
                case "ph":
                    Scrapping("PH");
                    break;
                case "sg":
                    Scrapping("SG");
                    break;


            }


        }


        static void Scrapping(string Country )
        {


            GetAppSettingsFile();
            var sDAL = new ScrapDAL(_iconfiguration); 
            ScrapModel sModel = new ScrapModel();

            if (Country == "TH") { sModel.Country = "TH"; sModel.HostName = "https://shopee.co.th/"; sModel.ImageURL = "https://cf.shopee.co.th/file/"; }
            else if (Country == "ID") { sModel.Country = "ID"; sModel.HostName = "https://shopee.co.id/"; sModel.ImageURL = "https://cf.shopee.co.id/file/"; }
            else if (Country == "MY") { sModel.Country = "MY"; sModel.HostName = "https://shopee.com.my/"; sModel.ImageURL = "https://cf.shopee.com.my/file/"; }
            else if (Country == "VN") { sModel.Country = "VN"; sModel.HostName = "https://shopee.vn/"; sModel.ImageURL = "https://cf.shopee.vn/file/"; }
            else if (Country == "PH") { sModel.Country = "PH"; sModel.HostName = "https://shopee.ph/"; sModel.ImageURL = "https://cf.shopee.ph/file/"; }
            else if (Country == "SG") { sModel.Country = "SG"; sModel.HostName = "https://shopee.sg/"; sModel.ImageURL = "https://cf.shopee.sg/file/"; }


            //var dbClient = new MongoClient("mongodb://myUserAdmin:1qazXSW%40@168.138.189.225:27727/?authSource=admin&readPreference=primary&appname=MongoDB%20Compass&directConnection=true&ssl=false");
            var dbClient = new MongoClient(sDAL.CnnStrMongo);
            IMongoDatabase db = dbClient.GetDatabase("owl_"+ sModel.Country);
            var Coll_SessionInfo = db.GetCollection<BsonDocument>("promotion");
            var Coll_SkuPre = db.GetCollection<BsonDocument>("sku_pre");
            var Coll_SkuPost = db.GetCollection<BsonDocument>("sku_post");


            for (; ; )
            {
                try
                {

                   // string search = "220120";
                    var builder = Builders<BsonDocument>.Filter;

                    //var Sessionfilter = builder.Eq("sql_status", 1) & builder.Gt("post_scrap_count", 0) & builder.Regex("scrap_name", "^" + search + ".*");
                    
                    var Sessionfilter = builder.Eq("sql_status", 0) & builder.Gt("post_scrap_count", 0);
                    var Session_docs = Coll_SessionInfo.Find(Sessionfilter).ToList();
                    
                    foreach (BsonDocument Summry_doc in Session_docs)
                    {

                        sModel.Country = Summry_doc["country_code"].ToString();
                        sModel.PromotionId = Summry_doc["promotionid"].ToString();

                        if (Summry_doc["post_scrap_count"].IsBsonNull == true) sModel.Slot = 0; else sModel.Slot = Convert.ToInt32(Summry_doc["post_scrap_count"]);
                        
                        sModel.PostArrCount = Summry_doc["post_scrap_slots"].AsBsonArray.Count;


                        for (; ; )
                        {
                            sModel.PostArrCount = GetArrCount(sModel.PromotionId).Item2;
                            if (sModel.PostArrCount <= sModel.Slot && sModel.PostArrCount >0)
                            {
                                #region
                                for (int slot = 1; slot <= sModel.Slot; slot++)
                                {

                                    builder = Builders<BsonDocument>.Filter;
                                    var PostScrapfilter = builder.Eq("promotionid", Convert.ToDouble(sModel.PromotionId)) & builder.Eq("sql_status", 0) & builder.Eq("slot", slot);
                                    // var PostScrapfilter = builder.Eq("itemid", 4636236578) & builder.Eq("promotionid", Convert.ToDouble(sModel.PromotionId)) & builder.Eq("slot", slot);
                                    var Post_docs = Coll_SkuPost.Find(PostScrapfilter).ToList();
                                    //   SET ALL VALUE zero or Blank for SKU TABLE 
                                    if (Post_docs.Count == 0) continue;
                                    sModel.ShopID = ""; sModel.ItemID = "";
                                    sModel.SellerType = ""; sModel.UnixCTime = 0; sModel.ProductName = "";
                                    sModel.Star = 0; sModel.Rating = 0; sModel.TotalSold = 0;
                                    sModel.MonthlySold = 0; sModel.CatId = ""; sModel.CatName = "";
                                    sModel.PriceSlashMin = 0; sModel.PriceSlashMax = 0;
                                    sModel.PriceMin = 0; sModel.PriceMax = 0; sModel.PriceRange = "0";
                                    sModel.Stock = 0; sModel.Category1 = ""; sModel.Category2 = ""; sModel.Category3 = "";
                                    sModel.ImageURL = ""; sModel.SKUAvgPrice = 0; sModel.MonthlyRevenue = 0;
                                    sModel.ProductURL = ""; sModel.IsPreOrder = 0; sModel.Estimated_Days = 0;
                                    sModel.TierVariations = ""; sModel.PriceFS = 0; sModel.FSBalanceStock = 0;
                                    sModel.FSLatestSold = 0; sModel.VariationBal = 0;

                                    Decimal PriceDiv = Convert.ToDecimal(100000.0);
                                    foreach (BsonDocument Pre_doc in Post_docs)
                                    {

                                        sModel.ShopID = Pre_doc["shopid"].ToString();
                                        sModel.ItemID = Pre_doc["itemid"].ToString();
                                        //if (sModel.ItemID == "8873499600") break;
                                        var Cat_Details = GetCat_FS_Stock(sModel.PromotionId, sModel.ShopID, sModel.ItemID);
                                        try { sModel.CatId = Cat_Details.Item1; } catch (Exception ex) { Console.WriteLine(ex.ToString()); } // 18
                                        try { sModel.CatName = Cat_Details.Item2; } catch (Exception ex) { Console.WriteLine(ex.ToString()); } // 18
                                        try { sModel.PriceMin = Convert.ToDecimal(Pre_doc["price_min"]) / PriceDiv; } catch (Exception ex) { Console.WriteLine(ex.ToString()); }// 21
                                        try { sModel.PriceMax = Convert.ToDecimal(Pre_doc["price_max"]) / PriceDiv; } catch (Exception ex) { Console.WriteLine(ex.ToString()); } // 22
                                        try { sModel.PriceFS = Convert.ToDecimal(Pre_doc["price"]) / PriceDiv; } catch (Exception ex) { Console.WriteLine(ex.ToString()); }  // 58  //fs_sku.price
                                        try { if (Pre_doc["sold_out_in"].IsBsonNull == true) sModel.SoldOutTime = 0; else sModel.SoldOutTime = Convert.ToInt32(Pre_doc["sold_out_in"]); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                                        
                                        if (sModel.SoldOutTime > 0)
                                        {
                                            //try { sModel.FSBalanceStock = GetAllocatedStock_Sum(sModel.PromotionId, sModel.ItemID,0);   } catch (Exception ex) { Console.WriteLine(sModel.PromotionId + "," + sModel.ItemID + "," + sModel.ShopID + "," + ex.ToString()); }  //60
                                            try { sModel.FSBalanceStock = GetAllocatedStock(sModel.PromotionId, sModel.ShopID, sModel.ItemID).Item1; } catch (Exception ex) { Console.WriteLine(sModel.PromotionId + "," + sModel.ItemID + "," + sModel.ShopID + "," + ex.ToString()); }  //60
                                            try { sModel.FSLatestSold = sModel.FSBalanceStock;  } catch (Exception ex) { Console.WriteLine(ex.ToString()); }  //60
                                            try { sModel.VariationBal = 0; } catch (Exception ex) { Console.WriteLine(ex.ToString()); }  //60
                                        }
                                        else
                                        {
                                            try { sModel.FSBalanceStock = Convert.ToInt32(Pre_doc["flash_sale"]["flash_sale_stock"]); } catch (Exception ex) { Console.WriteLine(sModel.PromotionId + "," + sModel.ItemID + "," + sModel.ShopID + "," + ex.ToString()); }  //60
                                            try { sModel.FSLatestSold = (Convert.ToInt32(Pre_doc["flash_sale"]["flash_sale_stock"]) - Convert.ToInt32(Pre_doc["flash_sale"]["stock"])); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }  //60
                                            try { sModel.VariationBal = Convert.ToInt32(Pre_doc["flash_sale"]["stock"]); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }  //60

                                        }
                                        try { sModel.FSSlotStartTime = ConvertUnixToLocal(sModel.Country, Pre_doc["updated_at"].ToString()); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }  // 58  //fs_sku.price

                                        sModel.ModelID = "";
                                        sModel.VariationName = "";
                                        sModel.VariationPriceSlash = 0;
                                        sModel.VariationPrice = 0;
                                        sModel.Variation_Allocated_Stock = 0;
                                        sModel.VariationStock = 0;
                                        sModel.VariationUnitSold = 0;
                                        sModel.FSTotalSold = 0;
                                        sModel.VariationRevenue = 0;
                                        sModel.FSRevenue = 0;
                                        sModel.VariationAvgPrice = 0;
                                        sModel.SKUAvgPrice = 0;

                                        int mdlCnt = 0;
                                        // VARIATION DETAILS ...........................
                                        var allModels = Pre_doc["models"].AsBsonArray;
                                        foreach (var model in allModels)
                                        {

                                            try { sModel.ModelID = model["modelid"].ToString(); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }  // 38
                                            
                                            try
                                            { 
                                                if (((model["price_stocks"][0]["allocated_stock"]).IsBsonNull == true) && sModel.SoldOutTime > 0)
                                                {
                                                    //sModel.Variation_Allocated_Stock = GetAllocatedStock_Model(sModel.PromotionId, sModel.ItemID, sModel.ModelID, 0);
                                                    sModel.Variation_Allocated_Stock = GetAllocatedStockModel(sModel.PromotionId, sModel.ShopID, sModel.ItemID, sModel.ModelID).Item1;
                                                    sModel.VariationStock = 0;
                                                }
                                                else
                                                {
                                                    sModel.Variation_Allocated_Stock = Convert.ToInt32(model["price_stocks"][0]["allocated_stock"]);
                                                    try { sModel.VariationStock = Convert.ToInt32(model["stock"]); } catch (Exception ex) { Console.WriteLine(ex.ToString()); } // 42
                                                }
                                            } 
                                            catch (Exception ex) { Console.WriteLine(ex.ToString()); } // 42
                                            
                                            if (sModel.Variation_Allocated_Stock == 0) continue;
                                            
                                            mdlCnt++;
                                            try { sModel.VariationPriceSlash = Convert.ToDecimal(model["price_before_discount"]) / PriceDiv; } catch (Exception ex) { Console.WriteLine(ex.ToString()); } // 40
                                            try { sModel.VariationPrice = Convert.ToDecimal(model["price"]) / PriceDiv; } catch (Exception ex) { Console.WriteLine(ex.ToString()); } // 41
                                            try { sModel.VariationUnitSold = sModel.Variation_Allocated_Stock - sModel.VariationStock; } catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                                            try { sModel.FSTotalSold = sModel.FSTotalSold + sModel.VariationUnitSold; } catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                                            try { sModel.VariationRevenue = sModel.VariationUnitSold * sModel.VariationPrice; } catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                                            if (sModel.FSTotalSold > 0)
                                                try { sModel.VariationAvgPrice = sModel.VariationRevenue / sModel.FSTotalSold; } catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                                            else sModel.VariationAvgPrice = 0;
                                            try { sModel.SKUAvgPrice = sModel.VariationAvgPrice + sModel.SKUAvgPrice; } catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                                            try { sModel.FSRevenue = sModel.FSRevenue + sModel.VariationRevenue; } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                                            //  Update INTO  SKU MODEL TABLE
                                            if ((IsRecordExist_Sku_model(sModel.PromotionId, sModel.Country, sModel.ModelID, sModel.ItemID) == true) && sModel.VariationUnitSold >0)
                                                UpdateSku_model(sModel.Country, sModel.PromotionId, sModel.ModelID, sModel.ItemID, sModel.VariationUnitSold, sModel.FSLatestSold);

                                            // INSERT INTO FS_SKU MODEL TABLE
                                            if (IsRecordExist_FS_Sku_model(sModel.PromotionId, sModel.Country, sModel.ModelID, sModel.ItemID, slot) == false)
                                                SaveFS_Sku_Model(sModel.Country, sModel.PromotionId, sModel.ShopID, sModel.ItemID, sModel.ModelID, slot, sModel.VariationPriceSlash, sModel.VariationPrice, sModel.Variation_Allocated_Stock, sModel.VariationStock, sModel.VariationUnitSold, sModel.VariationRevenue, sModel.VariationBal, sModel.FSSlotStartTime);


                                            sModel.ModelID = "";
                                            sModel.VariationName = "";
                                            sModel.VariationPriceSlash = 0;
                                            sModel.VariationPrice = 0;
                                            sModel.VariationStock = 0;
                                            sModel.VariationUnitSold = 0;
                                            sModel.Variation_Allocated_Stock = 0;

                                        }
                                        if (mdlCnt != 0)
                                        {
                                            if (IsRecordExistPreScrap(sModel.PromotionId, sModel.Country, sModel.ShopID, sModel.ItemID) == true)
                                            UpdatePreScrap(sModel.Country, sModel.PromotionId, sModel.ShopID, sModel.ItemID);
                                            // INSERT and Update INTO POST SCRAP SKU TABLE 
                                            if (IsRecordExist_FS_Sku(sModel.PromotionId, sModel.Country, sModel.ShopID, sModel.ItemID) == false)
                                                SavePostScrap(sModel.Country, sModel.PromotionId, sModel.ShopID, sModel.ItemID, sModel.PriceSlashMin, sModel.PriceSlashMax, sModel.PriceMin, sModel.PriceMax, sModel.PriceFS, sModel.FSBalanceStock, sModel.FSLatestSold, sModel.FSRevenue, slot, sModel.CatName, sModel.CatId, sModel.SoldOutTime, sModel.UnknownID);
                                            else
                                                UpdatePostScrap(sModel.Country, sModel.PromotionId, sModel.ShopID, sModel.ItemID, sModel.FSLatestSold, sModel.FSRevenue, slot, sModel.SoldOutTime, sModel.UnknownID);
                                        }
                                        // UPDATE SQL STATUS  for POST according to SLOT
                                        var PostScrapItemfilter = builder.Eq("promotionid", Convert.ToInt64(sModel.PromotionId)) & builder.Eq("shopid", Convert.ToInt32(sModel.ShopID)) & builder.Eq("itemid", Convert.ToInt64(sModel.ItemID)) & builder.Eq("slot", slot);
                                        var updateItemSqlStatus = Builders<BsonDocument>.Update.Set("sql_status", 1);
                                        var result = Coll_SkuPost.UpdateOne(PostScrapItemfilter, updateItemSqlStatus);

                                    }


                                }
                                #endregion

                                if (sModel.PostArrCount == sModel.Slot)
                                {
                                    // UPDATE PROMOTION STATUS 1 to 2
                                    var SessionProIdfilter = builder.Eq("promotionid", Convert.ToInt64(sModel.PromotionId));
                                    var updateSesionSqlStatus = Builders<BsonDocument>.Update.Set("sql_status", 2);
                                    var result2 = Coll_SessionInfo.UpdateOne(SessionProIdfilter, updateSesionSqlStatus);
                                    
                                    break;
                                }

                            }
                            else
                            {
                                sModel.PostArrCount = GetArrCount(sModel.PromotionId).Item2;
                                continue;
                            }
                        }
                    }

                    #region
                    Tuple<int, int> GetArrCount(string ProID)
                    {
                        int slot = 0; int slotarr = 0;
                        try
                        {

                            var Summaryfilter = builder.Eq("promotionid", Convert.ToDouble(sModel.PromotionId)) & builder.Gt("post_scrap_count", 0);
                            var Summary_docs = Coll_SessionInfo.Find(Summaryfilter).ToList();
                            foreach (BsonDocument Summary_doc in Summary_docs)
                            {
                                if (Summary_doc["post_scrap_count"].IsBsonNull == true) slot = 0; else slot = Convert.ToInt32(Summary_doc["post_scrap_count"]);
                                 slotarr = Summary_doc["post_scrap_slots"].AsBsonArray.Count;
                            }
                            return new Tuple<int, int>(slot, slotarr);
                        }
                        catch { return new Tuple<int, int>(slot, slotarr); }

                    }
                    #endregion
                    ///  THIS REGION DETAILS ABOUT SUMMARY POST SCRAP COUNT AND ARRAY DETAILS

                    #region
                    Tuple<string, string > GetCat_FS_Stock(string ProID, string ShopID, string ItemID)
                    {
                        string catID = "0"; string catName = "N/A"; 
                        try
                        {

                            var PreScrapItemfilter = builder.Eq("promotionid", Convert.ToInt64(ProID)) & builder.Eq("shopid", Convert.ToInt32(ShopID)) & builder.Eq("itemid", Convert.ToInt64(ItemID));
                            var Pre_docs = Coll_SkuPre.Find(PreScrapItemfilter).ToList();

                            foreach (BsonDocument Pre_doc in Pre_docs)
                            {
                                catID = Pre_doc["fs_catid"].ToString();
                                catName = Pre_doc["fs_catname"].ToString();
                            }
                            return new Tuple<string, string>(catID, catName );
                        }
                        catch { return new Tuple<string, string>(catID, catName); }

                    }
                    #endregion
                    ///  THIS REGION DETAILS ABOUT SKU PRE DETAILS  COUNT AND ARRAY DETAILS



                    #region
                    Tuple<int> GetAllocatedStock(string PromoID, string ShopID, string ItemID )
                    {
                        int AlloctedStock = 0; 
                        try
                        {
                            var PreScrapItemfilter = builder.Eq("promotionid", Convert.ToInt64(PromoID)) & builder.Eq("shopid", Convert.ToInt32(ShopID)) & builder.Eq("itemid", Convert.ToInt64(ItemID)) & builder.Eq("slot", 0);
                            var Pre_docs = Coll_SkuPost.Find(PreScrapItemfilter).ToList();
                            foreach (BsonDocument Pre_doc in Pre_docs)
                            {
                                AlloctedStock = Convert.ToInt32(Pre_doc["flash_sale"]["flash_sale_stock"]);
                            }
                            return new Tuple<int>(AlloctedStock);
                        }
                        catch { return new Tuple<int>(AlloctedStock); }

                    }
                    #endregion


                    #region
                    Tuple<int> GetAllocatedStockModel(string PromoID, string ShopID, string ItemID, string ModelID)
                    {
                        int AlloctedStock_Model = 0;
                        try
                        {
                            var PreScrapItemfilter = builder.Eq("promotionid", Convert.ToInt64(PromoID))  & builder.Eq("shopid", Convert.ToInt32(ShopID)) & builder.Eq("itemid", Convert.ToInt64(ItemID)) & builder.Eq("slot", 0);
                            var Post_docs = Coll_SkuPost.Find(PreScrapItemfilter).ToList();
                            foreach (BsonDocument Pre_doc in Post_docs)
                            {
                                var allModels = Pre_doc["models"].AsBsonArray;
                                foreach (var model in allModels)
                                {
                                   if( model["modelid"].ToString() == ModelID)
                                    {
                                        if (model["price_stocks"][0]["allocated_stock"].IsBsonNull == true) continue;
                                        AlloctedStock_Model = Convert.ToInt32(model["price_stocks"][0]["allocated_stock"]);
                                        break;
                                    }
                                }
                            }

                            return new Tuple<int>(AlloctedStock_Model);
                        }
                        catch { return new Tuple<int>(AlloctedStock_Model); }

                    }
                    #endregion


                }
                catch { continue; }
            }
        }

        static void GetAppSettingsFile()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _iconfiguration = builder.Build();


        }

        static DateTime ConvertUnixToLocal(string Country, string UnixTime)
        {
            var ScrapDAL = new ScrapDAL(_iconfiguration);
            return ScrapDAL.ConvertUnixToLocal(Country, UnixTime);
        }

        static DateTime getCurrentTime(string Country)
        {
            var ScrapDAL = new ScrapDAL(_iconfiguration);
            return ScrapDAL.getCurrentTime(Country);
        }



        static bool IsRecordExistPreScrap(string PromotionID, string Cntry, string Shopid, string prodid)
        {
            var ScrapDAL = new ScrapDAL(_iconfiguration);
            return ScrapDAL.IsRecordExistPreScrap(PromotionID, Cntry, Shopid, prodid);
        }


        static bool IsRecordExist_Sku_model(string PromotionID, string Cntry, string Modelid, string prodid)
        {
            var ScrapDAL = new ScrapDAL(_iconfiguration);
            return ScrapDAL.IsRecordExist_Sku_model(PromotionID, Cntry, Modelid, prodid);
        }

        static void UpdateSku_model(string Country, string PromoID, string ModelID, string ProdID, Decimal FS_Sku_UnitSold, Decimal FS_SKU_TotalSold)
        {
            var ScrapDAL = new ScrapDAL(_iconfiguration);
            ScrapDAL.UpdateSku_model(Country, PromoID, ModelID, ProdID, FS_Sku_UnitSold, FS_SKU_TotalSold);
        }

        static void UpdatePreScrap(string Country, string PromoID, string ShopID, string ProdID)
        {
            var ScrapDAL = new ScrapDAL(_iconfiguration);
            ScrapDAL.UpdatePreScrap(Country, PromoID, ShopID, ProdID);
        }



        static bool IsRecordExist_FS_Sku(string PromotionID, string Cntry, string shopid, string prodid)
        {
            var ScrapDAL = new ScrapDAL(_iconfiguration);
            return ScrapDAL.IsRecordExist_FS_Sku(PromotionID, Cntry, shopid, prodid);
        }



        static void SavePostScrap(string Country, string PromotionId, string ShopID, string ItemID, Decimal PRICE_SLASH_MIN, Decimal PRICE_SLASH_MAX, Decimal MIN_PRICE, Decimal MX_PRICE, Decimal PRICE, int FSBalanceStock, int FSTotalSold, Decimal FSRevenue, int SlotCount, string FSCatname, string FsCatid, int SoldOutTime, int UnknownId)
        {
            var ScrapDAL = new ScrapDAL(_iconfiguration);
            ScrapDAL.SavePostScrap(Country, PromotionId, ShopID, ItemID, PRICE_SLASH_MIN, PRICE_SLASH_MAX,  MIN_PRICE,  MX_PRICE,  PRICE, FSBalanceStock, FSTotalSold, FSRevenue, SlotCount, FSCatname, FsCatid, SoldOutTime, UnknownId);
        }


        static void UpdatePostScrap(string Country, string PromotionId, string ShopID, string ItemID, int FSTotalSold, Decimal FSRevenue, int SlotCount, int SoldOutTime, int UnoknowId)
        {
            var ScrapDAL = new ScrapDAL(_iconfiguration);
            ScrapDAL.UpdatePostScrap(Country, PromotionId, ShopID, ItemID, FSTotalSold, FSRevenue, SlotCount, SoldOutTime, UnoknowId);

        }

        //  SAVE FS SKU MODEL 

        static bool IsRecordExist_FS_Sku_model(string PromotionID, string Cntry, string Modelid, string prodid, int slotCount)
        {
            var ScrapDAL = new ScrapDAL(_iconfiguration);
            return ScrapDAL.IsRecordExist_FS_Sku_model(PromotionID, Cntry, Modelid, prodid, slotCount);
        }

        static void SaveFS_Sku_Model(string Country, string PromotionId, string ShopID, string ItemID, string ModelID, int SlotCount, Decimal PRICE_SLASH, Decimal PRICE, int AllocatedStock, int Stock, int UnitSold, Decimal Revenue, int FSBalanceStock, DateTime ScrapTime)
        {
            var ScrapDAL = new ScrapDAL(_iconfiguration);
            ScrapDAL.SaveFS_Sku_Model( Country,  PromotionId,  ShopID,  ItemID, ModelID,  SlotCount,  PRICE_SLASH,  PRICE,  AllocatedStock,  Stock,  UnitSold,  Revenue,  FSBalanceStock,  ScrapTime);
        }


        static int GetAllocatedStock_Sum(string PromotionID,  string prodid, int Slot)
        {
            var ScrapDAL = new ScrapDAL(_iconfiguration);
            return ScrapDAL.GetAllocatedStock_Sum(PromotionID,  prodid, Slot);
        }

        static int GetAllocatedStock_Model(string PromotionID,  string prodid, string ModelID, int Slot)
        {
            var ScrapDAL = new ScrapDAL(_iconfiguration);
            return ScrapDAL.GetAllocatedStock_Model(PromotionID,  prodid, ModelID, Slot);
        }

    }

}
