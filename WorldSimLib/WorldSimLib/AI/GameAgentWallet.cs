using System;
using System.Collections.Generic;

namespace WorldSimLib
{
    public class GameAgentWallet
    {
        public Dictionary<GameCurrency, float> Currencies { get; private set; } = new Dictionary<GameCurrency, float>();

        public GameAgentWallet() { }

        public GameAgentWallet(GameCurrency currency, float amount)
        {
            if(currency == null)
                throw new ArgumentNullException("currency");

            if( !Currencies.TryAdd( currency, amount ) )
                Currencies[currency] += amount;
        }

        public GameAgentWallet( GameAgentWallet walletToCopy )
        {
            Currencies = new Dictionary<GameCurrency, float>(walletToCopy.Currencies);
        }

        public void AddAmount( GameCurrency currency, float amount )
        {
            if (currency == null)
                throw new ArgumentNullException("currency");
            if (Currencies.ContainsKey(currency))
                Currencies[currency] += amount;
            else
                Currencies.Add(currency, amount);
        }

        public void RemoveAmount( GameCurrency currency, float amount )
        {
            if (currency == null)
                throw new ArgumentNullException("currency");
            if (Currencies.ContainsKey(currency))
                Currencies[currency] -= amount;
            else
                Currencies.Add(currency, -amount);
        }

        public float GetAmount( GameCurrency currency )
        {
            if (currency == null)
                throw new ArgumentNullException("currency");
            if (Currencies.ContainsKey(currency))
                return Currencies[currency];
            return 0;
        }

        public float SetAmount( GameCurrency currency, float amount )
        {
            if (currency == null)
                throw new ArgumentNullException("currency");
            if (Currencies.ContainsKey(currency))
                Currencies[currency] = amount;
            else
                Currencies.Add(currency, amount);
            return amount;
        }

        public static GameAgentWallet operator +(GameAgentWallet wallet1, GameAgentWallet wallet2)
        {
            if (wallet2 == null)
                throw new ArgumentNullException("wallet2");

            // Create a new GameAgentWallet to hold the result
            GameAgentWallet resultWallet = new GameAgentWallet();

            // Start by copying all currencies from wallet1 to resultWallet
            foreach (var currency in wallet1.Currencies)
            {
                resultWallet.SetAmount(currency.Key, currency.Value);
            }

            // Add currencies from wallet2
            foreach (var currency in wallet2.Currencies)
            {
                if (resultWallet.Currencies.ContainsKey(currency.Key))
                {
                    resultWallet.Currencies[currency.Key] += currency.Value;
                }
                else
                {
                    // If a currency in wallet2 is not present in wallet1, add it to resultWallet
                    resultWallet.SetAmount(currency.Key, currency.Value);
                }
            }

            return resultWallet;
        }


        public static GameAgentWallet operator -(GameAgentWallet wallet1, GameAgentWallet wallet2)
        {
            if (wallet2 == null)
                throw new ArgumentNullException("wallet2");

            // Create a new GameAgentWallet to hold the result
            GameAgentWallet resultWallet = new GameAgentWallet();

            // Start by copying all currencies from wallet1 to resultWallet
            foreach (var currency in wallet1.Currencies)
            {
                resultWallet.SetAmount(currency.Key, currency.Value);
            }

            // Subtract currencies from wallet2
            foreach (var currency in wallet2.Currencies)
            {
                if (resultWallet.Currencies.ContainsKey(currency.Key))
                {
                    resultWallet.Currencies[currency.Key] -= currency.Value;
                }
                else
                {
                    // If a currency in wallet2 is not present in wallet1, then the result is negative
                    resultWallet.SetAmount(currency.Key, -currency.Value);
                }
            }

            return resultWallet;
        }


        public override string ToString()
        {
            string retStr = "Wallet: \n";

            foreach( var currency in Currencies )
            {
                retStr += currency.Key.Name + ": " + currency.Value.ToString("##.##") + "\n";
            }

            return retStr;
        }
    }
}