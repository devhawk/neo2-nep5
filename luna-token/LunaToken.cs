using System;
using System.Numerics;
using System.ComponentModel;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;

[assembly: ContractTitle("LunaToken")]
[assembly: ContractDescription("Sample NEP5 token for Neo 2")]
[assembly: ContractAuthor("Harry Pierson")]
[assembly: ContractEmail("harrypierson@hotmail.com")]
[assembly: Features(ContractPropertyState.HasStorage | ContractPropertyState.HasDynamicInvoke | ContractPropertyState.Payable)]

namespace DevHawk.Neo.Samples
{
    public class LunaToken : SmartContract
    {
        const string NAME = "LunaToken";
        const string SYMBOL = "LUNA";
        const byte DECIMALS = 8;
        static readonly BigInteger TOTAL_SUPPLY = new BigInteger("0000c16ff28623".HexToBytes()); // 100,000,000 * 10^8
        static readonly byte[] OWNER = "AcAYK2AyjGARiVS73jHuCxqUPa2BqqQYmh".ToScriptHash();
        static readonly byte[] ZERO_ADDRESS = "0000000000000000000000000000000000000000".HexToBytes();

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> OnTransfer;

        public static object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return Runtime.CheckWitness(GetOwner());
            }

            if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "name") return Name();
                if (operation == "symbol") return Symbol();
                if (operation == "decimals") return Decimals();
                if (operation == "totalSupply") return TotalSupply();
                if (operation == "balanceOf") return BalanceOf((byte[])args[0]);
                if (operation == "transfer") return Transfer((byte[])args[0], (byte[])args[1], (BigInteger)args[2], ExecutionEngine.CallingScriptHash);
                if (operation == "transferOwnership") return TransferOwnership((byte[])args[0]);
                if (operation == "getOwner") return GetOwner();
                if (operation == "deploy") return Deploy();
                if (operation == "isDeployed") return IsDeployed();
                if (operation == "upgrade") return Upgrade((byte[])args[0], (byte[])args[1], (byte)args[2], (byte)args[3], (string)args[4], (string)args[5], (string)args[6], (string)args[7], (string)args[8]);
            }

            return false;
        }

        [DisplayName("deploy")]
        public static bool Deploy()
        {
            if (!Runtime.CheckWitness(OWNER))
            {
                Runtime.Log("Only owner can deploy this contract.");
                return false;
            }

            if (IsDeployed())
            {
                Runtime.Log("Already deployed");
                return false;
            }

            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            contract.Put("totalSupply", TOTAL_SUPPLY);
            contract.Put("owner", OWNER);

            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            asset.Put(OWNER, TOTAL_SUPPLY);
            OnTransfer(null, OWNER, TOTAL_SUPPLY);

            return true;
        }

        [DisplayName("isDeployed")]
        public static bool IsDeployed()
        {
            // if totalSupply has value, means deployed
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            byte[] total_supply = contract.Get("totalSupply");
            return total_supply.Length != 0;
        }

        [DisplayName("decimals")]
        public static byte Decimals() => DECIMALS;

        [DisplayName("name")]
        public static string Name() => NAME;

        [DisplayName("symbol")]
        public static string Symbol() => SYMBOL;

        [DisplayName("totalSupply")]
        public static BigInteger TotalSupply()
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            return contract.Get("totalSupply").AsBigInteger();
        }

        [DisplayName("balanceOf")]
        public static BigInteger BalanceOf(byte[] account)
        {
            if (!IsAddress(account))
            {
                Runtime.Log("The parameter account SHOULD be a legal address.");
                return 0;
            }

            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            return asset.Get(account).AsBigInteger();
        }

        [DisplayName("transfer")]
        public static bool Transfer(byte[] from, byte[] to, BigInteger amount) => true;

        static bool Transfer(byte[] from, byte[] to, BigInteger amount, byte[] callscript)
        {
            if (!IsAddress(from) || !IsAddress(to))
            {
                Runtime.Log("The parameters from and to SHOULD be legal addresses.");
                return false;
            }

            if (amount <= 0)
            {
                Runtime.Log("The parameter amount MUST be greater than 0.");
                return false;
            }

            if (!IsPayable(to))
            {
                Runtime.Log("The to account is not payable.");
                return false;
            }

            if (!Runtime.CheckWitness(from) && from.AsBigInteger() != callscript.AsBigInteger())
            {
                // either the tx is signed by "from" or is called by "from"
                Runtime.Log("Not authorized by the from account");
                return false;
            }

            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            var fromAmount = asset.Get(from).AsBigInteger();
            if (fromAmount < amount)
            {
                Runtime.Log("Insufficient funds");
                return false;
            }

            if (from == to)
            {
                return true;
            }

            if (fromAmount == amount)
            {
                asset.Delete(from);
            }
            else
            {
                asset.Put(from, fromAmount - amount);
            }

            var toAmount = asset.Get(to).AsBigInteger();
            asset.Put(to, toAmount + amount);

            OnTransfer(from, to, amount);
            return true;
        }

        [DisplayName("transferOwnership")]
        public static bool TransferOwnership(byte[] newOwner)
        {
            if (!Runtime.CheckWitness(GetOwner()))
            {
                Runtime.Log("Only allowed to be called by owner.");
                return false;
            }

            if (!IsAddress(newOwner))
            {
                Runtime.Log("The parameter newOwner SHOULD be a legal address.");
                return false;
            }

            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            contract.Put("owner", newOwner);
            return true;
        }

        [DisplayName("getOwner")]
        public static byte[] GetOwner()
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            var owner = contract.Get("owner");
            return owner;
        }

        [DisplayName("upgrade")]
        public static bool Upgrade(byte[] newScript, byte[] paramList, byte returnType, byte cps,
            string name, string version, string author, string email, string description)
        {
            if (!Runtime.CheckWitness(GetOwner()))
            {
                Runtime.Log("Only allowed to be called by owner.");
                return false;
            }

            _ = Contract.Migrate(newScript, paramList, returnType, (ContractPropertyState)cps, name, version, author, email, description);
            Runtime.Log("contract upgraded");
            return true;
        }


        private static bool IsAddress(byte[] address)
        {
            return address.Length == 20 && address.AsBigInteger() != ZERO_ADDRESS.AsBigInteger();
        }

        private static bool IsPayable(byte[] to)
        {
            var c = Blockchain.GetContract(to);
            return c == null || c.IsPayable;
        }
    }
}
