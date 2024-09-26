using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIOrderConfirmation.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderConfirmationController :ControllerBase
    {
        [HttpGet]
        public IActionResult GetSortComplete()
        {
            var response = new
            {
                SORT_COMPLETE = new
                {
                    wcs_id = "WCS_ID",
                    wh_id = "WH_ID",
                    msg_id = "MSG_ID",
                    trandt = "YYYYMMDDHHMISS",
                    SORT_COMP_SEG = new
                    {
                        LOAD_HDR_SEG = new
                        {
                            LODNUM = "SRCLOD",
                            LOAD_DTL_SEG = new[]
                            {
                                new
                                {
                                    subnum = "SUBNUM",
                                    dtlnum = "DTLNUM",
                                    stoloc = "DSTLOC"
                                }
                            }
                        }
                    }
                }
            };

            return Ok(response);
        }
    }
}
