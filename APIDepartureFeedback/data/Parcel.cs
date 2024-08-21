using CustomerApiRestKardex.ClassesResponse;
using CustomerApiRestKardex.DBModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Web;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace APIDepartureFeedback.data
{
    public class Parcel
    {
        public string NroOt { get; set; }
        public double weight { get; set; }
        public double Dimension { get; set; }

        public double length { get; set; }

        public double width { get; set; }

        public double height { get; set; }


        public string Message { get; set; }

        /// <summary>
        /// NO USAR AskedForSorting era para la forma antigua del servicio
        /// </summary>
        /// <returns></returns>
        public Respuesta AskedForSorting()
        {

            Data DatosRespuesta = new Data();

            Respuesta resp = new Respuesta();


            ChilexpressEntities BD = new ChilexpressEntities();
            TransportOrder transportOrder = new TransportOrder();
            try
            {
                long orExtraida = Int64.Parse(this.NroOt.Substring(10, 11));

                // transportOrder = BD.TransportOrder.Where(x => x.NroOt == this.NroOt).SingleOrDefault();

                transportOrder = BD.TransportOrder.Where(x => x.NroOt == orExtraida).SingleOrDefault();
                if (transportOrder != null)
                {

                    DatosRespuesta.Departure = transportOrder.departure;
                    DatosRespuesta.SortingType = "ALGO AQUI";
                    DatosRespuesta.LogisticsChannelName = "ALGO AQUI";
                    DatosRespuesta.LogisticsName = "ALGO AQUI";

                    transportOrder.AskedForSorting = true;
                    transportOrder.SortingASKDate = DateTime.Now;
                    BD.SaveChanges();

                    this.Message = "Correctamente Editado";
                }
                else
                {
                    this.Message = "transportOrder No encontrado en la BD Editado";
                    resp.msg = "transportOrder No encontrado en la BD Editado";
                }


            }
            catch (Exception e)
            {
                this.Message = e.Message;
                resp.msg = e.Message;
            }

            resp.data = DatosRespuesta;

            return resp;

        }

        /// <summary>
        /// AskedForSorting2 es para la version que utiliza las 2 tablas de chilexpress
        /// </summary>
        /// <returns></returns>
        public Respuesta AskedForSorting2()
        {

            Data DatosRespuesta = new Data();

            Respuesta resp = new Respuesta();

            ChilexpressEntities BD = new ChilexpressEntities();

            try
            {

                string otExtraida = this.NroOt.ToString().Substring(10, 12);

                long otAbuscar = 0;
                try
                {
                    otAbuscar = Int64.Parse(otExtraida);

                    TablaEntrada T_entrada = BD.TablaEntrada.Where(x => x.OT == otAbuscar).First();

                    if (T_entrada != null)
                    {
                        DatosRespuesta.Departure = T_entrada.BAJADA;
                        DatosRespuesta.SortingType = "ALGO AQUI";
                        DatosRespuesta.LogisticsChannelName = "ALGO AQUI";
                        DatosRespuesta.LogisticsName = "ALGO AQUI";

                        T_entrada.SINCRONIZADO = 1;

                        BD.SaveChanges();

                        this.Message = "Correctamente Editado";
                    }
                    else
                    {
                        this.Message = "OT No encontrada en la BD ";
                        resp.msg = "OT No encontrada en la BD";
                    }

                }
                catch (Exception)
                {

                    this.Message = "Codigo a escanear no puede tener algo distinto a un numero LONG";
                }




            }
            catch (Exception e)
            {
                this.Message = e.Message;
                resp.msg = e.Message;
            }

            resp.data = DatosRespuesta;

            return resp;

        }

        public Respuesta ObtenerSalidaClasificacion(int salida)
        {
            Respuesta resp = new Respuesta();

            Data data = new Data();
            data.Departure = salida;

            resp.data = data;
            resp.msg = "msg";

            return resp;

        }




        public void AsignarPesoYDimension()
        {

            ChilexpressEntities BD = new ChilexpressEntities();

            TransportOrder transportOrder = new TransportOrder();


            try
            {
                long orExtraida = Int64.Parse(this.NroOt.Substring(10, 11));

                transportOrder = BD.TransportOrder.Where(x => x.NroOt == orExtraida).SingleOrDefault();

                transportOrder.weight = this.weight;
                transportOrder.Dimension = this.Dimension;

                BD.SaveChanges();

                this.Message = "Correctamente Editado";

            }
            catch (Exception e)
            {
                this.Message = e.Message;
            }

        }

    }
    public class ParcelExitConfirmation
    {
        public long NroOt { get; set; }
        public int DepartureTaken { get; set; }


        /// <summary>
        ///  NO USAR
        /// </summary>
        /// <returns></returns>
        public Respuesta AsignarSalidaConfirmada()
        {

            ChilexpressEntities BD = new ChilexpressEntities();
            TransportOrder transportOrder = new TransportOrder();

            Respuesta resp = new Respuesta();

            try
            {
                transportOrder = BD.TransportOrder.Where(x => x.NroOt == this.NroOt).SingleOrDefault();

                transportOrder.DepartureTaken = this.DepartureTaken;
                transportOrder.DepartureTakenDate = DateTime.Now;


                BD.SaveChanges();

                resp.msg = "saved Succesfully";
            }
            catch (Exception e)
            {
                resp.msg = e.Message;
            }

            return resp;
        }

        /// <summary>
        /// AsignarSalidaConfirmada2 es para la version que utiliza las 2 tablas de chilexpress
        /// </summary>
        /// <returns></returns>
        public Respuesta AsignarSalidaConfirmada2()
        {

            ChilexpressEntities BD = new ChilexpressEntities();
            TablaSalida T_Salida = new TablaSalida();



            Respuesta resp = new Respuesta();

            try
            {
                string otExtraida = this.NroOt.ToString().Substring(10, 12);

                long ot = Int64.Parse(otExtraida);



                T_Salida.OT = ot;
                T_Salida.BAJADA = this.DepartureTaken;
                T_Salida.FECHA_SORTER = DateTime.Now;
                T_Salida.INFO_VCHAR = this.NroOt.ToString();

                BD.TablaSalida.Add(T_Salida);
                BD.SaveChanges();

                resp.msg = "saved Succesfully";
            }
            catch (Exception e)
            {
                resp.msg = e.Message;
            }

            return resp;
        }


        /// <summary>
        /// NO USAR
        /// </summary>
        /// <returns></returns>
        public Respuesta SP_AsignarSalidaConfirmada()
        {
            ChilexpressEntities BD = new ChilexpressEntities();
            Respuesta resp = new Respuesta();

            try
            {
                BD.SP_Confirmation(this.NroOt, this.DepartureTaken);

                BD.SaveChanges();

                resp.msg = "saved Succesfully";
            }
            catch (Exception e)
            {
                resp.msg = e.Message;
            }

            return resp;
        }


    }

}