using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebService.Models;
using WebService.Models_HRA;
using WebService.Request;
using WebService.Services;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace WebService.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    //TODO DESCOMENTAR ESTO
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ApoloHRAController: ControllerBase
    {
        private readonly ILogger<ApoloHRAController> _logger;
        private readonly EsmeraldaContext _db;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="db"></param>
        /// <param name="clientFactory"></param>
        /// <param name="configuration"></param>
        public ApoloHRAController(
            ILogger<ApoloHRAController> logger,
            EsmeraldaContext db,
            IHttpClientFactory clientFactory,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _db = db;
            _clientFactory = clientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// Usado para verificar que el servicio responde al llamado.
        /// </summary>
        /// <returns>Devuelve un valor verdadero si el servicio responde</returns>
        [HttpGet]
        [Route("echoping")]
        public ActionResult<bool> EchoPing()
        {
            return Ok(true);
        }

        /// <summary>
        /// Recupera un usuario existente en el monitor 
        /// </summary>
        /// <remarks>
        /// Recupera un usuario del monitor dado el RUN
        /// 
        /// Solicitud de ejemplo:
        /// 
        ///     POST /apolohra/user
        ///     {
        ///       "run": 12345678
        ///     }
        /// 
        /// </remarks>
        /// <param name="users"></param>
        /// <returns></returns>
        /// <response code="200">Devuelve la información del usuario</response>
        /// <response code="400">Mensaje descriptivo del error</response>
        [HttpPost]
        //TODO DESCOMENTAR LOS [Authorize] del controlador
        [Authorize] 
        [Route("user")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(users))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        public IActionResult GetUser([FromBody] users users)
        {
            try
            {
                var cred = _db.users.FirstOrDefault(a => a.run == users.run);
                //if()

                return Ok(cred);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Usuario no encontrado user:{users}", users);
                return BadRequest("Error.... Intente más tarde." + e);
            }
        }

        /// <summary>
        /// Recupera la lista de usuarios esmeralda asociados al laboratorio
        /// </summary>
        /// <remarks>
        /// Recupera la lista de usuarios esmeralda asociados al HRA
        /// 
        /// Solicitud de ejemplo:
        /// 
        ///     GET /apolohra/getUsers
        /// 
        /// </remarks>
        /// <param name="laboratoryId">Identificador del laboratorio en el monitor</param>
        /// <returns>retorna una lista de usuarios de esmeralda asociada al HRA</returns>
        /// <response code="200">Devuelve la información del usuario</response>
        /// <response code="400">Mensaje descriptivo del error</response>
        [HttpGet]
        //TODO DESCOMENTAR LOS [Authorize] del controlador
        [Authorize] 
        [Route("getUsers")]
        [ProducesResponseType(typeof(List<users>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public List<users> GetUsers(long laboratoryId)
        {
            try
            {
                List<users> usuarios = _db.users.Where(x => x.laboratory_id == laboratoryId).OrderBy(x => x.name).ToList();
                return usuarios;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "No se puede recuperar paciente:{buscador}");
                return new List<users>();
            }
        }

        /// <summary>
        /// Recupera el identificador interno de un paciente en el monitor esmeralda.
        /// </summary>
        /// <remarks>
        /// La búsqueda se realiza por el run del paciente o por otro identificador.
        ///
        /// Solicitudes de ejemplo:
        ///
        ///     POST /apolohra/getpatient_id
        ///     {
        ///         "run": "12838526"
        ///     }
        /// 
        /// </remarks>
        /// <param name="pa">Estructura con el identificador del paciente</param>
        /// <returns>El identficador interno, en caso de no encontrar devuelve un nulo</returns>
        /// <response code="200">Identificador interno del paciente en el monitor</response>
        /// <response code="400">Mensaje descriptivo del error</response>
        [HttpPost]
        [Authorize]
        [Route("getPatient_ID")]
        [ProducesResponseType(typeof(int?), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public IActionResult GetPatientId([FromBody] PacienteHRA pa)
        {
            try
            {
                Patients p;
                if (string.IsNullOrEmpty(pa.run))
                    p = _db.patients.FirstOrDefault(a => a.other_identification.Equals(pa.other_Id));
                else
                    p = _db.patients.FirstOrDefault(a => a.run.Equals(int.Parse(pa.run)));

                return p != null ? Ok(p.id) : Ok(null);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Paciente no recuperado, paciente:{pa}", pa);
                return BadRequest("Error.... Intente más tarde." + e);
            }
        }

        /// <summary>
        /// Agrega paciente inexistente en el monitor esmeralda
        /// </summary>
        /// <remarks>
        /// Ejemplo de solicitud:
        ///
        ///     POST /apolohra/addpatients
        ///     {
        ///         "run": 11111111,
        ///         "dv": "1",
        ///         "name": "Javier Andrés",
        ///         "fathers_family": "Mandiola",
        ///         "mothers_family": "Ovalle",
        ///         "gender": "male",
        ///         "birthday": "1975-04-03",
        ///         "status": "",
        ///         "created_at": "2020-10-28T12:00:00"
        ///         "updated_at": "2020-10-28T12:00:00"
        ///     }
        ///  
        /// </remarks>
        /// <param name="patients">Datos del paciente que serán ingresados</param>
        /// <returns>
        /// Identificador interno del paciente creado
        /// </returns>
        /// <response code="200">El identificador interno del paciente creado</response>
        /// <response code="400">Mensaje detallado del error</response>
        [HttpPost]
        [Authorize]
        [Route("AddPatients")]
        [ProducesResponseType(typeof(int?), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public IActionResult AddPatients([FromBody] Patients patients)
        {
            try
            {
                _db.patients.Add(patients);
                _db.SaveChanges();

                return Ok(patients.id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Paciente no guardado, paciente:{patients}", patients);
                return BadRequest("Error.... Intente más tarde." + " Error:" + e);
            }
        }

        /// <summary>
        /// Recupera la comuna indicando el código DEIS del MINSAL.
        /// </summary>
        /// <remarks>
        /// Ejemplo solicitud:
        ///
        ///     POST /apolohra/getcomuna
        ///     "2101"
        /// 
        /// </remarks>
        /// <param name="codeIds">Código DEIS del establecimiento.</param>
        /// <returns>Devuelve la comuna</returns>
        /// <response code="200">Devuelve la comuna asociada al código DEIS</response>
        /// <response code="400">Mensaje detallado del error</response>
        /// <response code="401">No está autenticado</response>
        [HttpPost]
        [Authorize]
        [Route("getComuna")]
        [ProducesResponseType(typeof(Communes), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public ActionResult<Communes> GetComuna([FromBody] string codeIds)
        {
            try
            {
                var c = _db.communes
                           .FirstOrDefault(x => x.code_deis.Equals(codeIds));
                return Ok(c);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Comuna con error, comuna id:{code_ids}", codeIds);
                return BadRequest("Error.... Intente más tarde.");
            }
        }

        /// <summary>
        /// Agrega datos demográficos al paciente.
        /// </summary>
        /// <remarks>
        /// Agrega información de la residencia y contacto del paciente
        ///
        /// Ejemplo solicitud:
        ///
        ///     POST /agregarhra/adddemograph
        ///     {
        ///         "street_type": "Calle"
        ///         "address": "Avelino Contardo",
        ///         "number": "1092",
        ///         "department": "104",
        ///         "nationality": "Chile",
        ///         "commune_id": 12,
        ///         "region_id": 2,
        ///         "latitude": -23.62272150,
        ///         "longitude": -70.38984400,
        ///         "telephone": "552244405",
        ///         "email": "test@mail.cl",
        ///         "patient_id": 1,
        ///         "created_at": "2020-11-28T12:00:00",
        ///         "updated_at": "2020-11-28T12:00:00"
        ///     }
        /// </remarks>
        /// <param name="demographics">Datos demográficos del paciente</param>
        /// <returns>Un mensaje de existo de la operacion</returns>
        /// <response code="200">Un mensaje del éxito de la operación</response>
        /// <response code="400">Mensaje detallado del error</response>
        /// <response code="401">No está autenticado</response>
        [HttpPost]
        [Authorize]
        [Route("AddDemograph")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public IActionResult AddDemograph([FromBody] demographics demographics)
        {
            try
            {
                _db.demographics.Add(demographics);
                _db.SaveChanges();
                return Ok("Se Guardo Correctamente la Demografía");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Demografico no agregado, demographics:{demographics}", demographics);
                return BadRequest("Error.....Intente más Tarde" + e);
            }
        }

        /// <summary>
        /// Agrega una nueva sospecha de COVID al monitor esmeralda
        /// </summary>
        /// <remarks>
        /// Ejemplo de solicitud:
        ///
        ///     POST /agregarhra/addsospecha
        ///     {
        ///         "gender": "male",
        ///         "age": 45,
        ///         "sample_at": "2020-10-27T08:30:00",
        ///         "epidemiological_week": 7,
        ///         "run_medic": "22222222",
        ///         "symptoms": "Si",
        ///         "symptoms_at": "2020-10-25T00:00:00",
        ///         "pscr_sars_cov_2": "pending",
        ///         "sample_type": "TÓRULAS NASOFARÍNGEAS",
        ///         "epivigila": 1024,
        ///         "gestation": false,
        ///         "gestation_week": null,
        ///         "close_contact": true,
        ///         "functionary": true,
        ///         "patient_id": 1,
        ///         "laboratory_id": 3,
        ///         "establishment_id": 3799,
        ///         "user_id": 1,
        ///         "created_at": "2020-10-28T09:00:00",
        ///         "updated_at": "2020-10-28T09:00:00"
        ///     }
        /// </remarks>
        /// <param name="sospecha">Información que es necesaria para la creación de la sospecha</param>
        /// <returns>Devuelve el número del caso de sospecha</returns>
        /// <response code="200">El número del caso de sospecha en el monitor</response>
        /// <response code="400">Mensaje detallado del error</response>
        /// <response code="401">No autenticado</response>
        [HttpPost]
        [Authorize]
        [Route("addSospecha")]
        [ProducesResponseType(typeof(int?), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddSospecha([FromBody] Sospecha sospecha)
        {
            try
            {
                var suspectCase = new SuspectCase
                {
                    age = sospecha.age,
                    gender = sospecha.gender,// sexo del paciente
                    sample_at = sospecha.sample_at, //fecha muestra
                    epidemiological_week = sospecha.epidemiological_week,
                    run_medic = sospecha.run_medic,//rut medico solicitante
                    symptoms = sospecha.symptoms == "Si",
                    pcr_sars_cov_2 = sospecha.pscr_sars_cov_2,
                    sample_type = sospecha.sample_type,// tipo de muestra
                    epivigila = sospecha.epivigila,
                    gestation = sospecha.gestation,
                    gestation_week = sospecha.gestation_week,
                    close_contact = sospecha.close_contact,
                    functionary = sospecha.functionary,
                    patient_id = sospecha.patient_id,//rut del paciente
                    establishment_id = sospecha.establishment_id,
                    user_id = sospecha.user_id,
                    created_at = sospecha.created_at,
                    updated_at = sospecha.updated_at,
                    laboratory_id = sospecha.laboratory_id
                };

                await _db.suspect_cases.AddAsync(suspectCase);
                await _db.SaveChangesAsync();

                var laboratorio = await _db.laboratories.FirstOrDefaultAsync(a => a.id == sospecha.laboratory_id);

                if (laboratorio == null) return BadRequest("Laboratorio no encontrado");

                if (!laboratorio.minsal_ws) return Ok(suspectCase.id);

                //variables para obtener los datos solicitados por Minsal 
                var pacienteId = suspectCase.patient_id;
                var paciente = await _db.patients.FindAsync(pacienteId);
                var demografia = await _db.demographics.FirstOrDefaultAsync(a => a.patient_id == pacienteId);
                var comuna = await _db.communes.FindAsync(demografia.commune_id);
                var pais = await _db.countries.FirstOrDefaultAsync(a => a.name == demografia.nationality);
                var responsable = await _db.users.FindAsync(suspectCase.user_id);
                string tipodoc;

                //validacion para el tipo de documento
                if (paciente.run != null)
                {
                    if (paciente.dv == null)
                    {
                        int suma = 0;
                        var pacienteRun = paciente.run.ToString();
                        var pacienteRunLength = pacienteRun.Length;
                        
                        for (int x = pacienteRunLength; x >= 0; x--)
                            suma += (pacienteRun[x]) * (((pacienteRunLength - x) % 6) + 2);

                        int numericDigito = (11 - suma % 11);
                        string digito = numericDigito == 11? "0": numericDigito == 10? "K": numericDigito.ToString();
                        paciente.dv = digito;
                    }

                    tipodoc = "RUN";
                }
                else
                {
                    tipodoc = "PASAPORTE";
                }

                //comienzo de el armado de json para crear muestra en Minsal
                var muestras = new List<MuestraMinsal>();

                muestras.Add(new MuestraMinsal
                {
                    codigo_muestra_cliente = suspectCase.id.ToString(),
                    epivigila = suspectCase.epivigila.ToString(),
                    id_laboratorio = laboratorio.id_openagora,
                    rut_responsable = responsable.run + "-" + responsable.dv,
                    paciente_tipodoc = tipodoc,
                    paciente_nombres = paciente.name,
                    paciente_ap_pat = paciente.fathers_family,
                    paciente_ap_mat = paciente.mothers_family,
                    paciente_fecha_nac = ((DateTime)paciente.birthday).ToString("dd-MM-yyyy"),
                    paciente_comuna = comuna.code_deis,
                    paciente_direccion = (demografia.address + " - " + demografia.number),
                    paciente_telefono = Convert.ToInt64(demografia.telephone),
                    paciente_sexo = paciente.gender == "male"? "M": "F",
                    cod_deis = _db.establishments.Find(suspectCase.establishment_id).new_code_deis,
                    fecha_muestra = ((DateTime)suspectCase.sample_at).ToString("dd-MM-yyyyTHH:mm:ss"),
                    tecnica_muestra = "RT-PCR",
                    tipo_muestra = suspectCase.sample_type,
                    paciente_run = paciente.run.ToString(),
                    paciente_dv = paciente.dv,
                    paciente_prevision = "FONASA",
                    paciente_pasaporte = paciente.other_identification,
                    paciente_ext_paisorigen = pais.id_minsal

                });

                //conexion hacia el end point de Minsal
                var httpClient = _clientFactory.CreateClient("conexionApiMinsal");
                httpClient.DefaultRequestHeaders.Add("ACCESSKEY", laboratorio.token_ws);
                var response = await httpClient.PostAsJsonAsync("crearMuestras_v2", muestras);

                //Se obtiene el status del response para guardar el retorno, ya sea la ID de muestra o el error Minsal
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.NoContent:
                        var respuesta = await response.Content.ReadAsAsync<List<respuestaMuestraMinsal>>();
                        suspectCase.minsal_ws_id = respuesta.First().id_muestra;
                        break;
                    default:
                        //Guardar error MINSAL en BD esmeralda
                        var error = await response.Content.ReadAsAsync<ErrorMinsal>();
                        suspectCase.ws_minsal_message = error.error;
                        break;
                }
                await _db.SaveChangesAsync();

                return Ok(suspectCase.id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Sospecha no agregada, sospecha:{sospecha}", sospecha);
                return BadRequest("No se guardo correctamente...." + e);
            }
        }

        /// <summary>
        /// Informa la recepción de la muestra por parte del laboratorio
        /// </summary>
        /// <remarks>
        /// Ejemplo de solicitud:
        ///
        ///     POST /apolohra/recepcionmuestra
        ///     {
        ///         "id": 1,
        ///         "reception_at": "2020-10-28T18:00:00",
        ///         "receptor_id": 1,
        ///         "laboratory_id": 3,
        ///         "updated_at": "2020-10-28T18:00"
        ///     }
        /// 
        /// </remarks>
        /// <param name="sospecha">Datos de la recepción de la muestra</param>
        /// <returns>Un mensaje del resultado de la operacion</returns>
        /// <response code="200">Un mensaje que la recepción se realizó</response>
        /// <response code="400">Mensaje detallado del error</response>
        /// <response code="401">No autenticado</response>
        [HttpPost]
        [Authorize]
        [Route("recepcionMuestra")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UdpateSospecha([FromBody] Sospecha sospecha)
        {
            try
            {
                var sospechaActualizada = await _db.suspect_cases.FindAsync(sospecha.id);

                if (sospechaActualizada == null) return BadRequest("No se guardo correctamente....");

                sospechaActualizada.reception_at = sospecha.reception_at;
                sospechaActualizada.receptor_id = sospecha.receptor_id;
                sospechaActualizada.laboratory_id = sospecha.laboratory_id;
                sospechaActualizada.updated_at = sospecha.updated_at;

                await _db.SaveChangesAsync();

                //Se obtiene el laboratorio para sacer el ACCESSKEY 
                var laboratorio = _db.laboratories.FirstOrDefault(a => a.id == sospechaActualizada.laboratory_id);

                if (laboratorio == null) return BadRequest("Laboratorio no encontrado");
                
                if(!laboratorio.minsal_ws) return Ok("Se Guardo correctamente...");
                    
                //Se prepara el json de la recepcion con la id del Minsal
                var recepcionesMinsal = new List<RecepcionMinsal>();

                recepcionesMinsal.Add(new RecepcionMinsal
                {
                    id_muestra = sospechaActualizada.minsal_ws_id
                });

                //conexion hacia el end point de Minsal
                var httpClient = _clientFactory.CreateClient("conexionApiMinsal");
                httpClient.DefaultRequestHeaders.Add("ACCESSKEY", laboratorio.token_ws);
                var response = await httpClient.PostAsJsonAsync("recepcionarMuestra", recepcionesMinsal);

                //Se obtiene el status del response para guardar el retorno en caso de error Minsal
                if (response.StatusCode != HttpStatusCode.OK &&
                    response.StatusCode != HttpStatusCode.NoContent)
                {
                    //Guardar error MINSAL en BD esmeralda
                    var error = await response.Content.ReadAsAsync<ErrorMinsal>();
                    sospechaActualizada.ws_minsal_message = error.error;
                    await _db.SaveChangesAsync();
                }

                return Ok("Se Guardo correctamente...");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Sospecha no actualizada, sospeche:{sospecha}", sospecha);
                return BadRequest("No se guardo correctamente....");
            }
        }

        /// <summary>
        /// Informa la entrega del resultado de la muestra al caso de asospecha
        /// </summary>
        /// <remarks>
        /// Ejemplo de la solicitud:
        ///
        ///     POST /apolohra/resultado
        ///     {
        ///         "id": 1,
        ///         "pscr_sars_cov_2_at": "2020-08-29T10:30:22",
        ///         "pscr_sars_cov_2": "negative",
        ///         "validator_id": 1,
        ///         "updated_at": "2020-08-29T10:30:22"
        ///     }
        ///
        /// </remarks>
        /// <param name="sospecha">Datos de la entrega del resultado</param>
        /// <returns></returns>
        /// <response code="200">Un mensaje que se registró el resultado de la muestra</response>
        /// <response code="400">Mensaje detallado del error</response>
        /// <response code="401">No autenticado</response>
        [HttpPost]
        [Authorize]
        [Route("resultado")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UdpateResultado([FromBody] Sospecha sospecha)
        {
            try
            {
                var sospechaActualizada = _db.suspect_cases.Find(sospecha.id);

                if (sospechaActualizada == null) return NotFound(sospecha);

                sospechaActualizada.pcr_sars_cov_2_at = sospecha.pscr_sars_cov_2_at;
                sospechaActualizada.pcr_sars_cov_2 = sospecha.pscr_sars_cov_2;
                sospechaActualizada.validator_id = sospecha.validator_id;
                sospechaActualizada.updated_at = sospecha.updated_at;

                await _db.SaveChangesAsync();

                var laboratorio = await _db.laboratories.FirstOrDefaultAsync(a => a.id == sospechaActualizada.laboratory_id);

                if (laboratorio == null) return BadRequest("Laboratorio no encontrado");
                
                if(!laboratorio.minsal_ws) return Ok("Exito... se actualizo los resultado..");

                //El resultado desde esmeralda viene como negative, positive o rejected, en ese caso Minsal lo recibe como Positivo, Negativo o Muestra no apta
                var resultadoEme = sospechaActualizada.pcr_sars_cov_2 switch
                {
                    "negative" => "Negativo",
                    "positive" => "Positivo",
                    _ => "Muestra no apta"
                };

                //Se obtiene el laboratorio para sacer el ACCESSKEY 

                //Se prepara el json del resultado
                var resultado = new ResultadoMinsal
                {
                    id_muestra = sospechaActualizada.minsal_ws_id,
                    resultado = resultadoEme
                };

                //Se hace un get a esmeralda para obtener el token del formulario 
                var tokenLogin = await GetTokenLogin();

                //conexion para iniciar sesion en esmeralda
                var apiEme = _clientFactory.CreateClient("conexionEsmeralda");
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("_token", tokenLogin),
                    new KeyValuePair<string, string>("email",  _configuration["ESMERALDA_USER"]),
                    new KeyValuePair<string, string>("password", _configuration["ESMERALDA_PASSWORD"])
                });
                
                await apiEme.PostAsync("login", formContent);

                //Una vez logueados se obtiene el pdf de resultado de la muestra, se convierte a array de bytes para ser enviado
                var responsePdf = await apiEme.GetAsync("lab/print/" + sospechaActualizada.id);
                var ms = new MemoryStream();
                await responsePdf.Content.CopyToAsync(ms);
                var bytesPdf = ms.ToArray();

                //conexion hacia el end point de Minsal
                var httpClient = _clientFactory.CreateClient("conexionApiMinsal");
                httpClient.DefaultRequestHeaders.Add("ACCESSKEY", laboratorio.token_ws);
                //Se arma el multipart/form-data con el json de el resultado y el pdf convertido para ser enviados a Minsal
                var jsonResultado = JsonConvert.SerializeObject(resultado);

                var form = new MultipartFormDataContent();
                var contentJsonResultado = new StringContent(jsonResultado);

                form.Add(new ByteArrayContent(bytesPdf, 0, bytesPdf.Length), "upfile", "document.pdf");
                form.Add(contentJsonResultado, "parametros");

                var response = await httpClient.PostAsync("entregaResultado", form);

                //Se obtiene el status del response para guardar el retorno en caso de error Minsal
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.NoContent:
                        await response.Content.ReadAsAsync<RespuestaResultadoMinsal>();
                        sospechaActualizada.ws_pntm_mass_sending = false;
                        sospechaActualizada.ws_minsal_message = "Muestra informada";
                        break;
                    default:
                        //Guardar error MINSAL en BD esmeralda
                        var error = await response.Content.ReadAsAsync<ErrorMinsal>();
                        sospechaActualizada.ws_minsal_message = error.error;
                        break;
                }
                await _db.SaveChangesAsync();
                
                return Ok("Exito... se actualizo los resultado..");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Resultado no actualizado, sospecha:{sospecha}", sospecha);
                return BadRequest("No se guardo correctamente....");
            }
        }

        /// <summary>
        /// Recupera el paciente dado un run u otro identificador
        /// </summary>
        /// <remarks>
        /// Ejemplo de solicitud
        ///
        ///     GET /apolohra/getpatients
        ///     "11111111"
        /// 
        /// </remarks>
        /// <param name="buscador">RUN u otro identificador</param>
        /// <returns>Paciente</returns>
        /// <response code="200">Información del paciente</response>
        /// <response code="400">Mensaje detallado del error</response>
        /// <response code="401">No autenticado</response>
        [HttpGet]
        [Authorize]
        [Route("getPatients")]
        [ProducesResponseType(typeof(Patients), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public IActionResult GetPatients([FromBody] string buscador)
        {
            try
            {
                var paciente = RecuperarPaciente(buscador);
                return Ok(paciente);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "No se puede recuperar paciente:{buscador}", buscador);
                return BadRequest("No se Encontro Paciente.... problema" + e);
            }
        }

        /// <summary>
        /// Recupera todos los casos de sospechas de un paciente.
        /// </summary>
        /// <remarks>
        /// El parámetro de la solicitud debe ser el RUN sin digito verificador u otro
        /// identificador (Pasaporte,etc)
        /// Ejemplo de solicitud:
        ///
        ///     GET /apolohra/getsospecha
        ///     "11111111"
        /// 
        /// </remarks>
        /// <param name="buscador">RUN o DNI del paciente a consultar</param>
        /// <returns>Un listado con los casos de sospecha que el paciente tiene</returns>
        /// <response code="200">Un listado con los casos de sospechas asociados al paciente</response>
        /// <response code="400">Mensaje detallado del error</response>
        /// <response code="401">No autenticado</response>
        [HttpGet]
        [Authorize]
        [Route("getSospecha")]
        [ProducesResponseType(typeof(List<Sospecha>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public IActionResult GetSospeha([FromBody] string buscador)
        {
            try
            {
                var paciente = RecuperarPaciente(buscador);
                var sospecha = _db.suspect_cases.Where(c => c.patient_id.Equals(paciente.id))
                                  .Select(
                                       s => new Sospecha
                                       {
                                           id = s.id,
                                           age = s.age,
                                           gender = s.gender,
                                           sample_at = s.sample_at,
                                           epidemiological_week = s.epidemiological_week,
                                           run_medic = s.run_medic,
                                           symptoms = s.symptoms.HasValue ? s.symptoms.Value ? "Si" : "No" : "No",
                                           pscr_sars_cov_2 = s.pcr_sars_cov_2,
                                           pscr_sars_cov_2_at = s.pcr_sars_cov_2_at,
                                           sample_type = s.sample_type,
                                           epivigila = s.epivigila,
                                           gestation = s.gestation,
                                           gestation_week = s.gestation_week,
                                           close_contact = s.close_contact,
                                           functionary = s.functionary,
                                           patient_id = s.patient_id,
                                           establishment_id = s.establishment_id,
                                           user_id = s.user_id,
                                           created_at = s.created_at,
                                           updated_at = s.updated_at,
                                           symptoms_at = s.symptoms_at,
                                           observation = s.observation
                                       }
                                   ).ToList();
                return Ok(sospecha);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "No se pudo recuperar sospecha del paciente:{buscador}", buscador);
                return BadRequest("No se Encontro sospecha.... problema" + e);
            }
        }

        /// <summary>
        /// Recupera los datos demográficos del paciente
        /// </summary>
        /// <remarks>
        /// El parámetro de la solicitud debe ser el RUN sin digito verificador u otro
        /// identificador (Pasaporte,etc)
        /// Ejemplo de solicitud:
        ///
        ///     GET /apolohra/getdemograph
        ///     "11111111"
        /// 
        /// </remarks>
        /// <param name="buscador">RUN u otro identificador del paciente</param>
        /// <returns>Datos demográficos del paciente</returns>
        /// <response code="200">Datos demográficos del paciente</response>
        /// <response code="400">Mensaje detallado del error</response>
        /// <response code="401">No autenticado</response>
        [HttpGet]
        [Authorize]
        [Route("getDemograph")]
        [ProducesResponseType(typeof(demographics), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public IActionResult GetDemograph([FromBody] string buscador)
        {
            try
            {
                var paciente = RecuperarPaciente(buscador);
                var demographic = _db.demographics.FirstOrDefault(c => c.patient_id.Equals(paciente.id));
                return Ok(demographic);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "No se pudo recuperar demografico del paciente:{buscador}", buscador);
                return BadRequest("No se Encontro sospecha.... problema" + e);
            }
        }

        private Patients RecuperarPaciente(string buscador)
        {
            var run = int.Parse(buscador);
            var paciente = _db.patients.FirstOrDefault(c => c.run.Equals(run));
            if (paciente == null)
            {
                paciente = _db.patients.FirstOrDefault(c => c.other_identification.Equals(buscador));
            }

            return paciente;
        }

        /// <summary>
        /// Recupera el caso de sospecha con sus datos relacionados
        /// </summary>
        /// <remarks>
        /// Recupera el caso de sospecha dado el número del caso.
        /// Ejemplo de solicitud
        ///
        ///     POST /apolohra/getsuspectcase
        ///     1
        /// 
        /// </remarks>
        /// <param name="idCase">Número del caso</param>
        /// <response code="200">Datos del caso de sospecha</response>
        /// <response code="400">Mensaje detallado del error</response>
        /// <response code="401">No autenticado</response>
        [HttpPost]
        [Authorize]
        [Route("getSuspectCase")]
        [ProducesResponseType(typeof(CasoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public IActionResult GetSuspectCase([FromBody] long idCase)
        {
            try
            {
                var caso = _db.suspect_cases.FirstOrDefault(x => x.id == idCase);
                if (caso == null)
                {
                    return BadRequest("No existe el caso");
                }
                var patient = _db.patients.FirstOrDefault(x => x.id == caso.patient_id);
                if (patient == null)
                {
                    return BadRequest("No existe el paciente");
                }
                var demographic = _db.demographics.FirstOrDefault(x => x.patient_id == patient.id);
                if (demographic == null)
                {
                    return BadRequest("No existe el demografico");
                }
                object retorno = new CasoResponse
                {
                    caso = new Sospecha
                    {
                        id = caso.id,
                        sample_at = caso.sample_at,
                        run_medic = caso.run_medic,
                        symptoms = caso.symptoms.HasValue ? caso.symptoms.Value ? "Si" : "No" : "No",
                        symptoms_at = caso.symptoms_at,
                        sample_type = caso.sample_type,
                        epivigila = caso.epivigila,
                        gestation = caso.gestation,
                        gestation_week = caso.gestation_week,
                        observation = caso.observation
                    },
                    paciente = patient,
                    demografico = demographic
                };

                return Ok(retorno);
            }
            catch (Exception e)
            {
                return BadRequest("Computer system error." + e);
            }
        }

        /// <summary>
        /// Se hace un Get al login de esmeralda para obtener el html del formulario
        /// </summary>
        private async Task<string> GetTokenLogin()
        {
            try
            {
                var apiEme = _clientFactory.CreateClient("conexionEsmeralda");
                var response = await apiEme.GetAsync("login");

                var loginPage = await response.Content.ReadAsStringAsync();
                const string textoInicio = "<input type=\"hidden\" name=\"_token\" value=\"";
                const string textoFin = "\">";
                var tokenlogin = GetTextoIntermedio(loginPage, textoInicio, textoFin);

                apiEme.Dispose();
                return tokenlogin;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "No se puede recuperar paciente:{buscador}");
                return null;
            }
        }

        /// <summary>
        /// Método para obtener un string especifico dentro de otro. 
        /// </summary>
        /// <remarks>
        /// Recupera el caso de sospecha dado el número del caso.
        /// Ejemplo de solicitud
        ///    Texto completo : "conexion api minsal"
        ///    Texto inicio : "conexion"
        ///    Texto fin : "minsal"
        ///    
        ///     Retorno = " api " 
        ///     
        /// </remarks>
        /// <param name="textoCompleto">Texto completo</param>
        /// <param name="textoInicio">Texto de referencia para el incio de la separación</param>
        /// <param name="textoFin">Texto de referencia para el fin de la separación</param>
        private string GetTextoIntermedio(string textoCompleto, string textoInicio, string textoFin)
        {
            var tokenLogin = "";
            if (textoCompleto.Contains(textoInicio) && textoCompleto.Contains(textoFin))
            {
                int inicio, fin;
                inicio = textoCompleto.IndexOf(textoInicio, 0, StringComparison.Ordinal) + textoInicio.Length;
                fin = textoCompleto.IndexOf(textoFin, inicio, StringComparison.Ordinal);
                tokenLogin = textoCompleto.Substring(inicio, fin - inicio);
            }
            return tokenLogin;
        }
    }
}
