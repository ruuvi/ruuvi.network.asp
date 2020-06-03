using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Core.Metadata.Edm;
using System.Globalization;
using System.Linq;
using System.Web;

namespace RuuviTagApp.ViewModels
{
    public class MacAddressModel
    {
        [Required(ErrorMessage = "Mac address is required.")]
        [ValidMacAddress(ErrorMessage = "Invalid mac address.")]
        public string MacAddress { get; set; }

        public string GetAddress() => MacAddress.Replace(":", string.Empty).Trim();
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ValidMacAddressAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            string macAddress = value as string;
            macAddress = macAddress.Trim();
            bool isValid = true;
            if (string.IsNullOrEmpty(macAddress) || macAddress.Length < 12)
            {
                isValid = false;
            }
            else if (macAddress.Length > 17)
            {
                isValid = false;
            }
            else if (!macAddress.All(c => ValidChar(c)))
            {
                isValid = false;
            }
            else if (macAddress.Length > 12 && !macAddress.Contains(':'))
            {
                isValid = false;
            }
            else if (macAddress.Length == 12 && !macAddress.Contains(':'))
            {
                string[] hexs = new string[6];
                for (int i = 0; i < macAddress.Length; i++)
                {
                    hexs[i / 2] += macAddress[i]; 
                }
                if (!ValidHexs(hexs))
                {
                    isValid = false;
                }
            }
            else
            {
                string[] hexs = macAddress.Split(':');
                if (!hexs.All(h => h.Length == 2))
                {
                    isValid = false;
                }
                if (!ValidHexs(hexs))
                {
                    isValid = false;
                }
            }
            return isValid;
        }

        private static bool ValidHexs(string[] hexs)
        {
            bool hexsAreValid = true;
            foreach (string hex in hexs)
            {
                if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                {
                    hexsAreValid = false;
                    break;
                }
            }
            return hexsAreValid;
        }

        private static bool ValidChar(char c)
        {
            string validChars = "ABCDEFabcdef0123456789:";
            bool charIsValid = true;
            if (!validChars.Contains(c))
            {
                charIsValid = false;
            }
            return charIsValid;
        }
    }
}