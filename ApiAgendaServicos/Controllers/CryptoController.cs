using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Cryptography;
using System.Text;

namespace ApiAgendaServicos.Controllers
{
    public class CryptoController : Controller
    {
        //Caracteres especiais utilizados
        private String[] Special = new String[] { "@", "#", "$", "!", "%", "-", "_" };

        //Requisitos mínimos de senha
        private Boolean useLowerCase;
        private Boolean useUpperCase;
        private Boolean useDigits;
        private Boolean useSpecial;

        public Boolean UseLowerCase { get { return useLowerCase; } set { useLowerCase = value; } }
        public Boolean UseUpperCase { get { return useUpperCase; } set { useUpperCase = value; } }
        public Boolean UseDigits { get { return useDigits; } set { useDigits = value; } }
        public Boolean UseSpecial { get { return useSpecial; } set { useSpecial = value; } }

        public CryptoController() : this(true, true, true) { }

        public CryptoController(Boolean UseLowerCase, Boolean UseUpperCase, Boolean UseDigits)
        {
            this.useLowerCase = UseLowerCase;
            this.useUpperCase = UseUpperCase;
            this.useDigits = UseDigits;
            this.useSpecial = UseSpecial;

            if ((!this.useLowerCase) && (!this.useUpperCase) && (!this.useDigits))
                this.useLowerCase = true;

        }

        public String GerarSenha(Int32 tamanhoSenha)
        {
            String passwd = "";

            Int32 uppper = 0;
            Int32 lower = 0;
            Int32 digits = 0;
            Int32 special = 0;

            Random rnd = new Random();

            while (passwd.Length < tamanhoSenha)
            {
                Int32 i = rnd.Next(1, 4);
                Int32 i2 = 0;

                //Regras de checagem de requisitos
                while (i != i2)
                {
                    switch (i)
                    {
                        case 1:
                            if (!useDigits)
                            {
                                i++;
                                break;
                            }
                            else if (digits > 0)
                            {
                                if ((useUpperCase) && (uppper == 0))
                                    i = i2 = 2;
                                else if ((useLowerCase) && (lower == 0))
                                    i = i2 = 3;
                                else if ((useSpecial) && (special == 0))
                                    i = i2 = 4;
                                else
                                    i2 = i;
                            }
                            else
                            {
                                i2 = i;
                            }
                            break;

                        case 2:
                            if (!useUpperCase)
                            {
                                i++;
                                break;
                            }
                            else if (uppper > 0)
                            {
                                if ((useDigits) && (digits == 0))
                                    i = i2 = 1;
                                else if ((useLowerCase) && (lower == 0))
                                    i = i2 = 3;
                                else if ((useSpecial) && (special == 0))
                                    i = i2 = 4;
                                else
                                    i2 = i;
                            }
                            else
                            {
                                i2 = i;
                            }
                            break;

                        case 3:
                            if (!useLowerCase)
                            {
                                i++;
                                break;
                            }
                            else if (lower > 0)
                            {
                                if ((useDigits) && (digits == 0))
                                    i = i2 = 1;
                                else if ((useUpperCase) && (uppper == 0))
                                    i = i2 = 2;
                                else if ((useSpecial) && (special == 0))
                                    i = i2 = 4;
                                else
                                    i2 = i;
                            }
                            else
                            {
                                i2 = i;
                            }
                            break;

                        case 4:
                            if (!useSpecial)
                            {
                                i++;
                                break;
                            }
                            else if (special > 0)
                            {
                                if ((useDigits) && (digits == 0))
                                    i = i2 = 1;
                                else if ((useUpperCase) && (uppper == 0))
                                    i = i2 = 2;
                                else if ((useLowerCase) && (lower == 0))
                                    i = i2 = 3;
                                else
                                    i2 = i;
                            }
                            else
                            {
                                i2 = i;
                            }
                            break;
                    }
                }

                String newItem = "";
                switch (i2)
                {
                    case 1:
                        newItem = ((char)(rnd.Next(48, 57))).ToString();
                        digits++;
                        break;

                    case 2:
                        newItem = ((char)(rnd.Next(65, 90))).ToString();
                        uppper++;
                        break;

                    case 3:
                        newItem = ((char)(rnd.Next(97, 122))).ToString();
                        lower++;
                        break;

                    case 4:
                        newItem = Special[rnd.Next(0, Special.Length - 1)];
                        special++;
                        break;
                }

                //Randomiza a posição dos caracteres
                if (passwd.Length > 0)
                {
                    System.Threading.Thread.Sleep(1);
                    Int32 pos = rnd.Next(0, passwd.Length - 1);
                    passwd = passwd.Insert(pos, newItem);
                }
                else
                {
                    passwd = newItem;
                }

                //Slep necessário para que o 'rnd.Next' traga valores diferentes
                System.Threading.Thread.Sleep(5);
            }

            return passwd;
        }

        [NonAction]
        public byte[] GerarHash(string value)
        {
            byte[] arrBytes = Encoding.ASCII.GetBytes(value);
            byte[] result;
            using (SHA256 shaM = new SHA256Managed())
            {
                result = shaM.ComputeHash(arrBytes);
            }
            return result;
        }

        [NonAction]
        public string GerarHashString(string value)
        {
            byte[] arrBytes = Encoding.ASCII.GetBytes(value);
            byte[] result;
            using (SHA256 shaM = new SHA256Managed())
            {
                result = shaM.ComputeHash(arrBytes);
            }
            //return result;

            StringBuilder Sb = new StringBuilder();
            foreach (Byte b in result)
                Sb.Append(b.ToString("x2"));

            return Sb.ToString();
        }
    }
}
