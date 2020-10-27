using System;
using System.Numerics;
using System.ComponentModel;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;

[assembly: Features(ContractPropertyState.HasStorage)]

namespace DevHawk.Neo.Samples
{
    public class LunaToken : SmartContract
    {
        static readonly byte[] Owner = "AXTE9ZeWPNULctmHk8ySZbDyFb3Mj5VKRw".ToScriptHash(); //Owner Address
        static readonly BigInteger total_amount = new BigInteger(10_000_000_000_000_000);


        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> OnTransfer;

        public static object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {

            }

            if (Runtime.Trigger == TriggerType.Application)
            {
                switch (operation)
                {
                    case "deploy":
                        return Deploy();
                    case "isDeployed":
                        return IsDeployed();
                    case "name":
                        return Name();
                    case "symbol":
                        return Symbol();
                    case "decimals":
                        return Decimals();
                    case "totalSupply":
                        return TotalSupply();
                    case "balanceOf":
                        return BalanceOf((byte[])args[0]);
                    case "transfer":
                        return Transfer((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);
                    case "transferOwnership":
                        return TransferOwnership((byte[])args[0]);
                    case "getOwner":
                        return GetOwner();
                    case "upgrade":
                        return Upgrade((byte[])args[0], (byte[])args[1], (byte)args[2], (ContractPropertyState)args[3], (string)args[4], (string)args[5], (string)args[6], (string)args[7], (string)args[8]);
                }
            }

            return false;
        }

        [DisplayName("deploy")]
        public static bool Deploy()
        {
            throw new NotImplementedException();
        }

        [DisplayName("isDeployed")]
        public static bool IsDeployed()
        {
            throw new NotImplementedException();
        }

        [DisplayName("decimals")]
        public static byte Decimals() => 8;

        [DisplayName("name")]
        public static string Name() => "Luna Token";

        [DisplayName("symbol")]
        public static string Symbol() => "LUNA";

        [DisplayName("totalSupply")]
        public static BigInteger TotalSupply()
        {
            throw new NotImplementedException();
        }

        [DisplayName("balanceOf")]
        public static BigInteger BalanceOf(byte[] account)
        {
            if (account.Length != 20)
            {
                Runtime.Log("BalanceOf() invalid address supplied");
                return 0;
            }

            StorageMap balances = Storage.CurrentContext.CreateMap(nameof(balances));
            BigInteger userBalance = balances.Get(account).AsBigInteger();
            if (userBalance < 0)
            {
                userBalance = 0;
            }
            return userBalance.AsByteArray().Concat(new byte[] { }).AsBigInteger();
        }

        [DisplayName("transfer")]
        public static bool Transfer(byte[] from, byte[] to, BigInteger amount)
        {
            if (from.Length != 20 || to.Length != 20)
            {
                Runtime.Log("Transfer() (from|to).Length != 20");
                return false;
            }

            if (amount < 0)
            {
                Runtime.Log("Transfer() invalid transfer amount must be >= 0");
                return false;
            }

            // retrieve balance of originating account
            BigInteger fromBalance = BalanceOf(from);
            if (fromBalance < amount)
            {
                Runtime.Log("Transfer() fromBalance < transferValue");
                // don't transfer if funds not available
                return false;
            }

            if (amount == 0 || from == to)
            {
                // don't accept a meaningless value
                Runtime.Log("Transfer() empty transfer amount or from==to");
                OnTransfer(from, to, amount);
                return true;    // as per nep5 standard - return true when amount is 0 or from == to
            }

            if (!Runtime.CheckWitness(from))
            {
                // ensure transaction is signed properly by the owner of the tokens
                Runtime.Log("Transfer() CheckWitness failed");
                return false;
            }

            BigInteger toBalance = BalanceOf(to);
            SetBalanceOf(from, fromBalance - amount);
            SetBalanceOf(to, toBalance + amount);

            OnTransfer(from, to, amount);
            return true;
        }

        static void SetBalanceOf(byte[] address, BigInteger newBalance)
        {
            if (address.Length != 20)
            {
                Runtime.Log("SetBalanceOf() address.length != 20");
                return;
            }

            StorageMap balances = Storage.CurrentContext.CreateMap(nameof(balances));

            if (newBalance <= 0)
            {
                balances.Delete(address);
            }
            else
            {
                Runtime.Notify("SetBalanceOf() setting balance", newBalance);
                balances.Put(address, newBalance);
            }
        }

        [DisplayName("transferOwnership")]
        public static bool TransferOwnership(byte[] newOwner)
        {
            throw new NotImplementedException();
        }

        [DisplayName("getOwner")]
        public static byte[] GetOwner()
        {
            throw new NotImplementedException();
        }

        [DisplayName("upgrade")]
        public static bool Upgrade(byte[] newScript, byte[] paramList, byte returnType, ContractPropertyState cps,
            string name, string version, string author, string email, string description)
        {
            if (!Runtime.CheckWitness(GetOwner()))
            {
                Runtime.Notify("Only allowed to be called by owner.");
                return false;
            }

            _ = Contract.Migrate(newScript, paramList, returnType, cps, name, version, author, email, description);
            Runtime.Notify("Proxy contract upgraded");
            return true;
        }

    }
}
