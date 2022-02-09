using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;


namespace MongoDBTest.DAL
{
    class ScrapDAL
    {
        private string CnnStr;
        public string CnnStrMongo;


        public ScrapDAL(IConfiguration iconfiguration)
        {
            CnnStr = iconfiguration.GetConnectionString("Default");
            CnnStrMongo = iconfiguration.GetConnectionString("Mongo");
        }



        SqlConnection cnn = new SqlConnection();
        SqlTransaction tran;

        // CONVERT  UNIX TO LOCAL  DATETIME

        public DateTime ConvertUnixToLocal(string Country, string UnixTime)
        {
            DateTimeOffset offset1 = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(UnixTime));
            if (Country == "MY" || Country == "PH" || Country == "SG")
            {
                return offset1.DateTime.ToLocalTime().AddHours(1);

            }

            else if (Country == "TH" || Country == "ID" || Country == "VN")
            {
                return offset1.DateTime.ToLocalTime();

            }
            return DateTime.Now;
        }


        public DateTime getCurrentTime(string Country)

        {
            if (Country == "MY" || Country == "PH" || Country == "SG")
            {
                return DateTime.Now.AddHours(1);
            }
            else
            {
                return DateTime.Now;
            }

        }




        ////////////////////// POST SCRAPPING SQL FUNCTIONS ///////////////////////





        public bool IsRecordExistPreScrap(string PromotionID, string Cntry, string Shopid, string prodid)
        {
            try
            {
                string Qry;
                DataTable dt = new DataTable();
                cnn.ConnectionString = CnnStr;
                if (cnn.State == ConnectionState.Closed) cnn.Open();
                Qry = "Select * from sku where country_code = '" + Cntry + "' and ShopId ='" + Shopid + "' and itemId ='" + prodid + "' and promotionid = '" + PromotionID + "' ";
                SqlDataAdapter Sqldbda = new SqlDataAdapter(Qry, cnn);
                Sqldbda.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    cnn.Close();
                    return true;
                }
                else
                {
                    cnn.Close();
                    return false;
                }
            }
            catch
            {
                cnn.Close();
                return false;
            }
        }

        public void UpdatePreScrap(string Country, string PromoID, string ShopID, string ProdID)
        {
            try
            {

                //  UPDATE SKU PRICE 
                cnn.ConnectionString = CnnStr;
                if (cnn.State == ConnectionState.Closed) cnn.Open();
                tran = cnn.BeginTransaction();
                string Qry = "Update sku set avg_price = (select sum(avg_price) as AvgPrice from sku_model where country_code = @country_code and promotionid =  @promotionid and shopid = @shopid and itemid = @itemid) , monthly_revenue = avg_price * monthly_sold where country_code = @country_code and promotionid =  @promotionid and shopid = @shopid and itemid = @itemid";
                SqlCommand Comm2 = new SqlCommand(Qry, cnn);
                Comm2.Parameters.Add("@country_code", SqlDbType.VarChar, 3).Value = Country;
                Comm2.Parameters.Add("@promotionid", SqlDbType.BigInt).Value = Convert.ToInt64(PromoID);  // MODIFY
                Comm2.Parameters.Add("@shopid", SqlDbType.BigInt).Value = Convert.ToInt64(ShopID);
                Comm2.Parameters.Add("@itemid", SqlDbType.BigInt).Value = Convert.ToInt64(ProdID);
                Comm2.Transaction = tran;
                Comm2.CommandTimeout = 0;
                Comm2.ExecuteNonQuery();
                Comm2.Dispose();
                tran.Commit();
                cnn.Close();
                Comm2.Parameters.Clear();



                //  UPDATE SKU PRICE 
                cnn.ConnectionString = CnnStr;
                if (cnn.State == ConnectionState.Closed) cnn.Open();
                tran = cnn.BeginTransaction();

                Qry = "Update sku set monthly_revenue = avg_price * monthly_sold where country_code = @country_code and promotionid =  @promotionid and shopid = @shopid and itemid = @itemid";
                Comm2.Parameters.Add("@country_code", SqlDbType.VarChar, 3).Value = Country;
                Comm2.Parameters.Add("@promotionid", SqlDbType.BigInt).Value = Convert.ToInt64(PromoID);  // MODIFY
                Comm2.Parameters.Add("@shopid", SqlDbType.BigInt).Value = Convert.ToInt64(ShopID);
                Comm2.Parameters.Add("@itemid", SqlDbType.BigInt).Value = Convert.ToInt64(ProdID);
                Comm2.Transaction = tran;
                Comm2.CommandTimeout = 0;
                Comm2.ExecuteNonQuery();
                Comm2.Dispose();
                tran.Commit();
                cnn.Close();
                Comm2.Parameters.Clear();


            }
            catch (SqlException ex)
            {

                tran.Rollback();
                cnn.Close();

            }
            catch (Exception ex)
            {
                //throw new Exception(ex.Message.ToString());
                tran.Rollback();
                cnn.Close();
            }

        }



        public bool IsRecordExist_Sku_model(string PromotionID, string Cntry, string Modelid, string prodid)
        {
            try
            {
                string Qry;
                DataTable dt = new DataTable();
                cnn.ConnectionString = CnnStr;
                if (cnn.State == ConnectionState.Closed) cnn.Open();
                Qry = "Select * from sku_model where country_code = '" + Cntry + "' and modelid ='" + Modelid + "' and itemId ='" + prodid + "' and promotionid = '" + PromotionID + "' ";
                SqlDataAdapter Sqldbda = new SqlDataAdapter(Qry, cnn);
                Sqldbda.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    cnn.Close();
                    return true;
                }
                else
                {
                    cnn.Close();
                    return false;
                }
            }
            catch
            {
                cnn.Close();
                return false;
            }
        }

        public void UpdateSku_model(string Country, string PromoID, string ModelID, string ProdID, Decimal FS_Sku_UnitSold, Decimal FS_SKU_TotalSold)
        {
            try
            {
                cnn.ConnectionString = CnnStr;
                if (cnn.State == ConnectionState.Closed) cnn.Open();
                tran = cnn.BeginTransaction();

                string Qry = "Update sku_model set revenue = price * @revenue, avg_price = revenue / @avg_price where country_code = @country_code and promotionid = @promotionid and itemid= @itemid and modelid = @modelid";
                SqlCommand Comm2 = new SqlCommand(Qry, cnn);
                Comm2.Parameters.Add("@country_code", SqlDbType.VarChar, 3).Value = Country;
                Comm2.Parameters.Add("@promotionid", SqlDbType.BigInt).Value = Convert.ToInt64(PromoID);
                Comm2.Parameters.Add("@itemid", SqlDbType.BigInt).Value = Convert.ToInt64(ProdID);
                Comm2.Parameters.Add("@modelid", SqlDbType.BigInt).Value = Convert.ToInt64(ModelID);
                Comm2.Parameters.Add("@revenue", SqlDbType.Decimal).Value = FS_Sku_UnitSold;
                Comm2.Parameters.Add("@avg_price", SqlDbType.Decimal).Value = FS_SKU_TotalSold;
                Comm2.Transaction = tran;
                Comm2.CommandTimeout = 0;
                Comm2.ExecuteNonQuery();
                Comm2.Dispose();
                tran.Commit();
                cnn.Close();
                Comm2.Parameters.Clear();


                cnn.ConnectionString = CnnStr;
                if (cnn.State == ConnectionState.Closed) cnn.Open();
                tran = cnn.BeginTransaction();

                Qry = "Update sku_model set avg_price = revenue / @avg_price where country_code = @country_code and promotionid = @promotionid and itemid= @itemid and modelid = @modelid";
                Comm2 = new SqlCommand(Qry, cnn);
                Comm2.Parameters.Add("@country_code", SqlDbType.VarChar, 3).Value = Country;
                Comm2.Parameters.Add("@promotionid", SqlDbType.BigInt).Value = Convert.ToInt64(PromoID);
                Comm2.Parameters.Add("@itemid", SqlDbType.BigInt).Value = Convert.ToInt64(ProdID);
                Comm2.Parameters.Add("@modelid", SqlDbType.BigInt).Value = Convert.ToInt64(ModelID);
                Comm2.Parameters.Add("@revenue", SqlDbType.Decimal).Value = FS_Sku_UnitSold;
                Comm2.Parameters.Add("@avg_price", SqlDbType.Decimal).Value = FS_SKU_TotalSold;
                Comm2.Transaction = tran;
                Comm2.CommandTimeout = 0;
                Comm2.ExecuteNonQuery();
                Comm2.Dispose();
                tran.Commit();
                cnn.Close();
                Comm2.Parameters.Clear();



            }
            catch (SqlException ex)
            {

                tran.Rollback();
                cnn.Close();

            }
            catch (Exception ex)
            {
                //throw new Exception(ex.Message.ToString());
                tran.Rollback();
                cnn.Close();
            }

        }

        public bool IsRecordExist_FS_Sku(string PromotionID, string Cntry, string shopid, string prodid)
        {
            try
            {
                string Qry;
                DataTable dt = new DataTable();
                cnn.ConnectionString = CnnStr;
                if (cnn.State == ConnectionState.Closed) cnn.Open();
                Qry = "Select * from fs_sku where country_code = '" + Cntry + "' and shopid ='" + shopid + "' and itemId ='" + prodid + "' and promotionid = '" + PromotionID + "' ";
                SqlDataAdapter Sqldbda = new SqlDataAdapter(Qry, cnn);
                Sqldbda.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    cnn.Close();
                    return true;
                }
                else
                {
                    cnn.Close();
                    return false;
                }
            }
            catch
            {
                cnn.Close();
                return false;
            }
        }

        public void SavePostScrap(string Country, string PromotionId, string ShopID, string ItemID, Decimal PRICE_SLASH_MIN, Decimal PRICE_SLASH_MAX, Decimal MIN_PRICE, Decimal MX_PRICE, Decimal PRICE, int FSBalanceStock, int FSTotalSold, Decimal FSRevenue, int SlotCount, string FSCatname, string FsCatid, int SoldOutTime, int UnknownId)
        {

            try
            {
                cnn.ConnectionString = CnnStr;
                if (cnn.State == ConnectionState.Closed) cnn.Open();
                tran = cnn.BeginTransaction();


                string Qry = "insert into fs_sku(country_code, promotionid, shopid, itemid, price_min, price_max, price, flash_sale_stock, fs_total_sold, fs_revenue, slot_count, fs_catname, fs_catid, sold_out_time, unknown_id)" +
                                "values(@country_code, @promotionid, @shopid, @itemid,  @price_min, @price_max, @price, @flash_sale_stock, @fs_total_sold, @fs_revenue, @slot_count, @fs_catname, @fs_catid, @sold_out_time,@unknown_id)";


                SqlCommand Comm2 = new SqlCommand(Qry, cnn);
                Comm2.Parameters.Add("@country_code", SqlDbType.VarChar, 3).Value = Country;
                Comm2.Parameters.Add("@promotionid", SqlDbType.BigInt).Value = Convert.ToInt64(PromotionId);
                Comm2.Parameters.Add("@shopid", SqlDbType.BigInt).Value = Convert.ToInt64(ShopID);
                Comm2.Parameters.Add("@itemid", SqlDbType.BigInt).Value = Convert.ToInt64(ItemID);
                Comm2.Parameters.Add("@price_min", SqlDbType.Decimal).Value = MIN_PRICE;
                Comm2.Parameters.Add("@price_max", SqlDbType.Decimal).Value = MX_PRICE;
                Comm2.Parameters.Add("@price", SqlDbType.Decimal).Value = PRICE;
                Comm2.Parameters.Add("@flash_sale_stock", SqlDbType.Int).Value = FSBalanceStock;
                Comm2.Parameters.Add("@fs_total_sold", SqlDbType.Int).Value = FSTotalSold;
                Comm2.Parameters.Add("@fs_revenue", SqlDbType.Decimal).Value = FSRevenue;
                Comm2.Parameters.Add("@slot_count", SqlDbType.Int).Value = SlotCount;
                Comm2.Parameters.Add("@fs_catname", SqlDbType.NVarChar, 255).Value = FSCatname;
                Comm2.Parameters.Add("@fs_catid", SqlDbType.Int).Value = Convert.ToInt64(FsCatid);
                Comm2.Parameters.Add("@sold_out_time", SqlDbType.Int).Value = SoldOutTime;
                Comm2.Parameters.Add("@unknown_id", SqlDbType.Int).Value = UnknownId;

                Comm2.Transaction = tran;
                Comm2.CommandTimeout = 0;

                Comm2.ExecuteNonQuery();
                Comm2.Dispose();
                tran.Commit();
                cnn.Close();

                Comm2.Parameters.Clear();

            }
            catch (SqlException ex)
            {
                tran.Rollback();
                cnn.Close();

            }
            catch (Exception ex)
            {
                cnn.Close();

            }


        }

        public void UpdatePostScrap(string Country, string PromotionId, string ShopID, string ItemID, int FSTotalSold, Decimal FSRevenue, int SlotCount, int SoldOutTime, int UnoknowId)
        {

            try
            {
                cnn.ConnectionString = CnnStr;
                if (cnn.State == ConnectionState.Closed) cnn.Open();
                tran = cnn.BeginTransaction();

                string Qry = "update fs_sku set fs_total_sold = @fs_total_sold, fs_revenue = @fs_revenue, slot_count = @slot_count, sold_out_time = @sold_out_time , unknown_id = @unknown_id where country_code = @country_code and promotionid = @promotionid and shopid = @shopid and itemid = @itemid";
                SqlCommand Comm2 = new SqlCommand(Qry, cnn);
                Comm2.Parameters.Add("@country_code", SqlDbType.VarChar, 3).Value = Country;
                Comm2.Parameters.Add("@promotionid", SqlDbType.BigInt).Value = Convert.ToInt64(PromotionId);
                Comm2.Parameters.Add("@shopid", SqlDbType.BigInt).Value = Convert.ToInt64(ShopID);
                Comm2.Parameters.Add("@itemid", SqlDbType.BigInt).Value = Convert.ToInt64(ItemID);
                Comm2.Parameters.Add("@fs_total_sold", SqlDbType.Int).Value = FSTotalSold;
                Comm2.Parameters.Add("@fs_revenue", SqlDbType.Decimal).Value = FSRevenue;
                Comm2.Parameters.Add("@slot_count", SqlDbType.Int).Value = SlotCount;
                Comm2.Parameters.Add("@sold_out_time", SqlDbType.Int).Value = SoldOutTime;
                Comm2.Parameters.Add("@unknown_id", SqlDbType.Int).Value = UnoknowId;
                Comm2.Transaction = tran;
                Comm2.CommandTimeout = 0;

                Comm2.ExecuteNonQuery();
                Comm2.Dispose();
                tran.Commit();
                cnn.Close();
                Comm2.Parameters.Clear();



                //cnn.ConnectionString = CnnStr;
                //if (cnn.State == ConnectionState.Closed) cnn.Open();
                //tran = cnn.BeginTransaction();
                //Qry = "update fs_sku set fs_total_sold = (select  sum(unit_sold) as UnitSold from fs_sku_model where Promotionid = @promotionid and itemid = @itemid and slot_count = @slot_count) , fs_revenue = (select  sum(revenue) as Revenue from fs_sku_model where Promotionid = @promotionid and itemid = @itemid and slot_count = @slot_count)   where country_code = @country_code and promotionid = @promotionid and shopid = @shopid and itemid = @itemid";
                //Comm2 = new SqlCommand(Qry, cnn);
                //Comm2.Parameters.Add("@country_code", SqlDbType.VarChar, 3).Value = Country;
                //Comm2.Parameters.Add("@promotionid", SqlDbType.BigInt).Value = Convert.ToInt64(PromotionId);
                //Comm2.Parameters.Add("@itemid", SqlDbType.BigInt).Value = Convert.ToInt64(ItemID);
                //Comm2.Parameters.Add("@shopid", SqlDbType.BigInt).Value = Convert.ToInt64(ShopID);
                //Comm2.Parameters.Add("@slot_count", SqlDbType.Int).Value = SlotCount;

                //Comm2.Transaction = tran;
                //Comm2.CommandTimeout = 0;
                //Comm2.ExecuteNonQuery();
                //Comm2.Dispose();
                //tran.Commit();
                //cnn.Close();
                //Comm2.Parameters.Clear();



            }
            catch (SqlException ex)
            {
                tran.Rollback();
                cnn.Close();

            }
            catch (Exception ex)
            {
                cnn.Close();

            }
        }

        public bool IsRecordExist_FS_Sku_model(string PromotionID, string Cntry, string Modelid, string prodid, int slotCount)
        {
            try
            {
                string Qry;
                DataTable dt = new DataTable();
                cnn.ConnectionString = CnnStr;
                if (cnn.State == ConnectionState.Closed) cnn.Open();
                Qry = "Select * from fs_sku_model where country_code = '" + Cntry + "' and modelid ='" + Modelid + "' and itemId ='" + prodid + "' and promotionid = '" + PromotionID + "' and slot_count = '" + slotCount + "' ";
                SqlDataAdapter Sqldbda = new SqlDataAdapter(Qry, cnn);
                Sqldbda.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    cnn.Close();
                    return true;
                }
                else
                {
                    cnn.Close();
                    return false;
                }
            }
            catch
            {
                cnn.Close();
                return false;
            }
        }

        public void SaveFS_Sku_Model(string Country, string PromotionId, string ShopID, string ItemID, string ModelID, int SlotCount, Decimal PRICE_SLASH, Decimal PRICE, int AllocatedStock, int Stock, int UnitSold, Decimal Revenue, int FSBalanceStock, DateTime ScrapTime)
        {

            try
            {
                if (AllocatedStock == 0) return;

                cnn.ConnectionString = CnnStr;
                if (cnn.State == ConnectionState.Closed) cnn.Open();
                tran = cnn.BeginTransaction();
                string Qry = "insert into fs_sku_model(country_code, promotionid, itemid, modelid, slot_count, price_slash, price, allocated_stock, stock, unit_sold, revenue, fs_balance_stock, scrap_time) " +
                               "values(@country_code, @promotionid, @itemid, @modelid, @slot_count, @price_slash, @price, @allocated_stock, @stock, @unit_sold, @revenue, @fs_balance_stock, @scrap_time)";

                SqlCommand Comm2 = new SqlCommand(Qry, cnn);
                Comm2.Parameters.Add("@country_code", SqlDbType.VarChar, 3).Value = Country;
                Comm2.Parameters.Add("@promotionid", SqlDbType.BigInt).Value = Convert.ToInt64(PromotionId);
                Comm2.Parameters.Add("@shopid", SqlDbType.BigInt).Value = Convert.ToInt64(ShopID);
                Comm2.Parameters.Add("@itemid", SqlDbType.BigInt).Value = Convert.ToInt64(ItemID);
                Comm2.Parameters.Add("@modelid", SqlDbType.BigInt).Value = Convert.ToInt64(ModelID);
                Comm2.Parameters.Add("@slot_count", SqlDbType.Int).Value = SlotCount;
                Comm2.Parameters.Add("@price_slash", SqlDbType.Decimal).Value = PRICE_SLASH;
                Comm2.Parameters.Add("@price", SqlDbType.Decimal).Value = PRICE;
                Comm2.Parameters.Add("@allocated_stock", SqlDbType.Int).Value = AllocatedStock;
                Comm2.Parameters.Add("@stock", SqlDbType.Int).Value = Stock;
                Comm2.Parameters.Add("@unit_sold", SqlDbType.Int).Value = UnitSold;
                Comm2.Parameters.Add("@revenue", SqlDbType.Decimal).Value = Revenue;
                Comm2.Parameters.Add("@fs_balance_stock", SqlDbType.Int).Value = FSBalanceStock;
                Comm2.Parameters.Add("@scrap_time", SqlDbType.DateTime).Value = ScrapTime;

                Comm2.Transaction = tran;
                Comm2.CommandTimeout = 0;

                Comm2.ExecuteNonQuery();
                Comm2.Dispose();
                tran.Commit();
                cnn.Close();

                Comm2.Parameters.Clear();

            }
            catch (SqlException ex)
            {
                tran.Rollback();
                cnn.Close();

            }
            catch (Exception ex)
            {
                cnn.Close();

            }
        }


        public int GetAllocatedStock_Sum(string PromotionID, string prodid, int Slot)
        {
            try
            {
                string Qry;
                DataTable dt = new DataTable();
                cnn.ConnectionString = CnnStr;
                if (cnn.State == ConnectionState.Closed) cnn.Open();
                Qry = "select Sum(allocated_stock) as AllocatedStock from fs_sku_model  where itemid = '" + prodid + "' and promotionid = " + PromotionID + " and slot_count = " + Slot + "";
                SqlDataAdapter Sqldbda = new SqlDataAdapter(Qry, cnn);
                Sqldbda.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    cnn.Close();
                    return Convert.ToInt32(dt.Rows[0][0].ToString());
                }
                else
                {
                    cnn.Close();
                    return 0;
                }
            }
            catch
            {
                cnn.Close();
                return 0;
            }
        }

        public int GetAllocatedStock_Model(string PromotionID, string prodid, string ModelID, int Slot)
        {
            try
            {
                string Qry;
                DataTable dt = new DataTable();
                cnn.ConnectionString = CnnStr;
                if (cnn.State == ConnectionState.Closed) cnn.Open();
                Qry = "select allocated_stock from fs_sku_model  where itemid = '" + prodid + "' and promotionid = " + PromotionID + " and modelid = '" + ModelID + "' and slot_count = " + Slot + "";
                SqlDataAdapter Sqldbda = new SqlDataAdapter(Qry, cnn);
                Sqldbda.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    cnn.Close();
                    return Convert.ToInt32(dt.Rows[0][0].ToString());
                }
                else
                {
                    cnn.Close();
                    return 0;
                }
            }
            catch
            {
                cnn.Close();
                return 0;
            }
        }

    }
}
