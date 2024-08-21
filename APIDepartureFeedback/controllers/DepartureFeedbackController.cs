using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using CustomerApiRestKardex.ClassesRequest;
using CustomerApiRestKardex.DBModels;
using CustomerApiRestKardex.Logger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;


namespace CustomerApiRestKardex.Controllers
{
    public class DepartureFeedbackController : ApiController
    {
        private static readonly log4net.ILog log = LogHelper.GetLogger();// log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //// GET: api/DepartureFeedback
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        //// GET: api/DepartureFeedback/5
        //public string Get(int id)
        //{
        //    return "value";
        //}

        // POST: api/DepartureFeedback[Swashbuckle.Swagger.Annotations.SwaggerResponseAttribute(HttpStatusCode.OK)]
        [Swashbuckle.Swagger.Annotations.SwaggerResponseAttribute(HttpStatusCode.Conflict)]
        [Swashbuckle.Swagger.Annotations.SwaggerResponseAttribute(HttpStatusCode.NoContent)]
        [ResponseType(typeof(Parcel))]
        public HttpResponseMessage Post([FromBody] Parcel parcel)
        {


            HttpResponseMessage response;
            string Json = JsonConvert.SerializeObject(parcel);
            //long asd = 11111111112222222222220;
            //long asd2 = 1111111122222222220;
            log.Info("Creacion de Order: " + Json);
            if (parcel != null)
            {

                SorterEntities sorter = new SorterEntities();

                var rows = sorter.Get_PosibleSalidas(parcel.NroOt);

                var primerRegistro = rows.First();
                double volumen = parcel.width * parcel.height * parcel.length;


                Detalle detalle = sorter.Detalle.Where(x => x.ID == primerRegistro.detalle_ID).First();
                detalle.Contador++;

                if (detalle.Contador == detalle.Cantidad_solicitada)
                {
                    detalle.Closed = true;
                }

                detalle.Suma_peso = detalle.Suma_peso + (decimal)parcel.weight;
                detalle.Suma_volumen = detalle.Suma_volumen + (decimal)volumen;
                detalle.Ultima_modificacion = DateTime.Now;

                Historica historica = new Historica();

                historica.Codigo_dun = primerRegistro.Codigo_dun;
                historica.Peso = (decimal)parcel.weight;

                historica.Volumen = (decimal)volumen;

                historica.width = (decimal)parcel.width;
                historica.height = (decimal)parcel.height;
                historica.length = (decimal)parcel.length;

                historica.Fecha_hora = DateTime.Now;
                historica.Salida_sorter = primerRegistro.Salida_sorter;
                historica.Detalle_ID = primerRegistro.detalle_ID;

                sorter.Historica.Add(historica);

                try
                {
                    sorter.SaveChanges();
                }
                catch (Exception)
                {
                    response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                    response.Content = new StringContent("PASO ALGO MALO");
                }

                //string responsetext = JsonConvert.SerializeObject(rows);
                string responsetext = JsonConvert.SerializeObject(parcel.ObtenerSalidaClasificacion(primerRegistro.Salida_sorter));

                response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(responsetext);
            }
            else
            {
                response = Request.CreateResponse(HttpStatusCode.NoContent);
                log.Error("CONFLICT");
            }
            return response;

        }

        //// PUT: api/DepartureFeedback/5
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        //// DELETE: api/DepartureFeedback/5
        //public void Delete(int id)
        //{
        //}
    }
}