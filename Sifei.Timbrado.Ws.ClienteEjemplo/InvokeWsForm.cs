using IniParser;
using IniParser.Model;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web.Services.Protocols;
using System.Windows.Forms;

/// <summary>
/// 
/// Clase ejemplo de consumo de timbrado
/// </summary>
namespace Sifei.Timbrado.Ws.ClienteEjemplo {
    public partial class InvokeWsForm : Form {

        public IniData Config { get; set; }
        public InvokeWsForm()
        {
            InitializeComponent();
            loadConnectionData();
        }

        public string TimbradoPath { get; set; }
        /// <summary>
        /// Este metodo obtiene los datos de conexion de un archivo .ini, en produccion NO hacer esto.
        /// </summary>        
        public void loadConnectionData()
        {

            var parser = new FileIniDataParser();
            Config = parser.ReadFile("./config.ini");
            PathCancelacion = "./cancelacion";
            TimbradoPath = "./timbrado";

            Directory.CreateDirectory(TimbradoPath);//me aseguro que exista

            //estructura de archivo ini.
            /**
            
             * 
             */
        }


        private void btnTimbrar_Click(object sender, EventArgs e)
        {
            var service = new Cliente.timbrado.SIFEIService();
            var usuario = Config["timbrado"]["UsuarioSIFEI"];
            var password = Config["timbrado"]["PasswordSIFEI"];
            var serie = "";
            var idEquipoGenerado = Config["timbrado"]["IdEquipoGenerado"];

            //leemos el archivo

            byte[] archivoXml= File.ReadAllBytes("./assets/cfdi.xml");
            //Ya que la respuesta devuelve un zip debemos de extraer en contenido
            try { 
            byte[] respuestaZip= service.getCFDI(
                usuario,
                password,
                archivoXml,
                serie,
                idEquipoGenerado
            );

                Stream zs = new MemoryStream(respuestaZip);
                Stream xmlS = new MemoryStream();

                //se extrae el xml, que es el unico archivo zip.
                using (ZipFile zip = ZipFile.Read(zs))
                {
                    foreach (ZipEntry ze in zip) {
                        ze.Extract(TimbradoPath);                        
                        break;                        
                    }
                }
                MessageBox.Show("CFDI timbrado");               

            }catch(SoapException ex)
            {
                MessageBox.Show(
                                    ex.Message +
                                    ex.Detail.FirstChild.SelectSingleNode("codigo").InnerText +

                                    ex.Detail.FirstChild.SelectSingleNode("error").InnerText


                                    , "Error", MessageBoxButtons.OK, MessageBoxIcon.Error
                                    );
            }


        }
        public string PathCancelacion { get; set; }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            var usuario = Config["timbrado"]["UsuarioSIFEI"];
            var password = Config["timbrado"]["PasswordSIFEI"];
            var idEquipoGenerado = Config["timbrado"]["IdEquipoGenerado"];

            //Para este ejemplo el pfx se lee desde el sistema de archivos, el origen puede ser cualquiera que sea seguro.
            var passwordPfx = Config["cancelacion"]["PFXPass"];
            var pfxBytes = File.ReadAllBytes(Config["cancelacion"]["PFX"]); // a manera de ejemlo se lee desde el siste de archivos
            var uuids = new List<string>();
            uuids.Add("5d100c8d-1b19-4e5c-9748-6116fdbe5465");

            var c = new Cancelacion.Ws.Client.Cancelacion.Cancelacion();
            //el metodo devuelve un acuse de cancelacion 

            try
            {
                var acuseXML = c.cancelaCFDI(
                      usuario, //usuario sifei
                      password, //contraseña
                      "RFC",
                      pfxBytes, //bytes del archivo pfx
                      passwordPfx, //pasword del pfx, por ende el pfx debe ser creado con contraseña
                      uuids.ToArray() //array del UUID de XML  a cancelar.
                 );
                //en este caso se ejemplifica como guardar el XML en una ruta
                Directory.CreateDirectory(PathCancelacion);
                String acuseFileName = Path.Combine(PathCancelacion, "acuseCancelacion" + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".xml");
                File.WriteAllText(acuseFileName, acuseXML, Encoding.UTF8);
                MessageBox.Show(string.Format("Cancelacion Exitosa, acuse guardado en {0}", acuseFileName));
            }
            catch (SoapException ex)
            {
                //obtenemos la excepcion
                MessageBox.Show(
                    ex.Message+
                    ex.Detail.FirstChild.SelectSingleNode("codigo").InnerText +

                    ex.Detail.FirstChild.SelectSingleNode("error").InnerText 


                    , "Error", MessageBoxButtons.OK, MessageBoxIcon.Error
                    );

            }
        }   
    }
}
