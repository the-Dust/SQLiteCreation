using SQLiteCreation.Context.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteCreation.Context
{
    class DBQuery : IDBQuery
    {
        public KeyValuePair<string, string> GetQuery(StandardQueries query)
        {
            string queryCondition1 = string.Format("{0}1 Вывести количество и сумму заказов по каждому продукту за {0}текущий месяц{0}", Environment.NewLine);
            string queryCondition2a = string.Format("{0}2a Вывести все продукты, которые были заказаны в текущем {0}месяце, но которых не было в прошлом.{0}", Environment.NewLine);
            string queryCondition2b = string.Format("{0}2b Вывести все продукты, которые были только в прошлом месяце,{0}но не в текущем, и которые были в текущем месяце, но не в прошлом.{0}", Environment.NewLine);
            string queryCondition3 = string.Format("{0}3 Помесячно вывести продукт, по которому была максимальная сумма{0}заказов за этот период, сумму по этому продукту и его долю от{0}общего объема за этот период.{0}", Environment.NewLine);

            string query1 = "select product.name as `Продукт`, count(*) as `Кол-во заказов`, round(sum(`order`.amount), 4) " +
                             "as `Сумма заказов`  from 'order' join product on `product`.id = `order`.product_id " +
                             "where dt_month = strftime('%Y-%m', 'now') group by product.name";

            string tempTable = "select distinct `product`.id as id1, product.name as name1 from 'order' " +
                                "join product on `order`.product_id = `product`.id " +
                                "where dt_month = strftime('%Y-%m', 'now'{0}) ";

            string subQuery = "with lastMonth as (" + string.Format(tempTable, ", '-1 month'") +
                                "), thisMonth as (" + string.Format(tempTable, "") + ") ";

            string query2a = subQuery + "select name1 as `Продукт` from (select * from thisMonth except select * from lastMonth)";

            string query2b = subQuery + "select name1 as `Прошлый месяц`, name2 as `Текущий месяц` from product " +
                            "left outer join (select * from lastMonth except select * from thisMonth) " +
                            "on product.id = id1 left outer join " +
                            "(select id1 as id2, name1 as name2 from thisMonth except select * from lastMonth) " +
                            "on product.id = id2 where name1 not null or name2 not null ";

            string query3 = "select period as `Период`, product_name as `Продукт`, round(max(total_amount),4) as `Сумма`, round(max(total_amount)*100/sum(total_amount),2) as `Доля,%` " +
                            "from(select strftime('%Y-%m', dt) as period, sum(amount) as total_amount, product.name as product_name from `order` " +
                            "join product on `product`.id = `order`.product_id group by product_id, dt_month order by dt_month asc) group by period; ";

            string queryResult;
            string queryCondition;

            switch (query)
            {
                case StandardQueries.First:
                    queryResult = query1;
                    queryCondition = queryCondition1;
                    break;
                case StandardQueries.SecondA:
                    queryResult = query2a;
                    queryCondition = queryCondition2a;
                    break;
                case StandardQueries.SecondB:
                    queryResult = query2b;
                    queryCondition = queryCondition2b;
                    break;
                case StandardQueries.Third:
                    queryResult = query3;
                    queryCondition = queryCondition3;
                    break;
                default:
                    queryResult = query1;
                    queryCondition = queryCondition1;
                    break;
            }

            return new KeyValuePair<string, string>(queryCondition, queryResult);
        }
    }
}
