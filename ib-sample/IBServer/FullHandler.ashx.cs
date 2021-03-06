/*
 * Copyright (c) 2014 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
 * in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software distributed under the License
 * is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions and limitations under
 * the License.
 */
using System;
using System.Web;
using System.Web.UI;
using System.IO;
using JWT;
using Newtonsoft.Json;
using InstantBuyLibrary;
using System.Collections.Generic;

namespace IBServer
{
    public class FullHandler : System.Web.IHttpHandler
    {
        public class Request
        {
            public Cart cart;
            public String jwt;
        }

        public virtual bool IsReusable
        {
            get
            {
                return false;
            }
        }

        public virtual void ProcessRequest(HttpContext context)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            String origin = Config.getOrigin(request);
            String arrCart = request["arrCart"];
            String tax = request["tax"];
            String shipping = request["shipping"];
            Double totalPrice = Double.Parse(request["totalPrice"]);
            String googleTransactionId = request["gid"];

            List<LineItem> itemList = JsonConvert.DeserializeObject<List<LineItem>>(arrCart);
            itemList.Add(new LineItem("Tax", tax, LineItem.Role.TAX));
            itemList.Add(new LineItem("Shipping Detail", shipping, LineItem.Role.SHIPPING));

            Cart cart = new Cart(Config.CURRENCY);
            foreach (var lineItem in itemList) {
                cart.AddItem(lineItem);
            }

            //Create Full Wallet Body
            WalletBody fwb = new WalletBody.FullWalletBuilder()
              .GoogleTransactionId(googleTransactionId)
              .ClientId(Config.getOauthClientId())
              .MerchantName(Config.getMerchantName())
              .Origin(Config.getOrigin(request))
              .Cart(cart)
              .Build();

            // Create Full Wallet request object
            JwtRequest fwr = new JwtRequest(JwtRequest.FULL_WALLET, Config.getMerchantId(), fwb);

            // Set the expiration time - not necessary but a useful example
            fwr.exp = Convert.ToInt64(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds) + 60000L;

            // Convert the JwtRequest object to a string
            String fullWalletJwt = JsonWebToken.Encode(fwr, Config.getMerchantSecret(), JwtHashAlgorithm.HS256);

            response.Write(fullWalletJwt);
        }
    }
}

