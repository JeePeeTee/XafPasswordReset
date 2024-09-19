#region MIT License
// ==========================================================
// 
// XafPasswordReset project - Copyright (c) 2024 JeePeeTee
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
// ===========================================================
#endregion

using System.Security.Cryptography;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.Persistent.BaseImpl.MultiTenancy;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Swashbuckle.AspNetCore.Annotations;
using XafPasswordReset.Module.BusinessObjects;

namespace XafPasswordReset.Blazor.Server.Controllers;

[ApiController]
[Route("api/")]
public class ResetPasswordController : ControllerBase {
    private readonly IXafApplicationProvider _applicationProvider;

    public ResetPasswordController(IXafApplicationProvider applicationProvider) {
        _applicationProvider = applicationProvider;
    }


    [HttpGet(nameof(ResetPassword))]
    [SwaggerOperation("Confirms user reset password")]
    // [Authorize]
    public IActionResult ResetPassword(Guid tenantId, Guid secret) {
        try {
            var application = _applicationProvider.GetApplication();
            
            var configuration = application.ServiceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration["ConnectionStrings:ConnectionString"];
            XpoDefault.DataLayer = XpoDefault.GetDataLayer(connectionString, AutoCreateOption.None);
            
            Tenant tenant;
            
            using (var unitOWork = new UnitOfWork()) {
                tenant = unitOWork.Query<Tenant>().Where(t => t.Oid == tenantId).First();
            }
            
            XpoDefault.DataLayer = XpoDefault.GetDataLayer(tenant.ConnectionString, AutoCreateOption.None);

            PasswordReset check;
            using (var unitOWork = new UnitOfWork()) {
                check = unitOWork.Query<PasswordReset>().Where(t => t.Oid == secret)!.First();
            }

            var actionResult = Validations(check);
            if(((ObjectResult) actionResult).StatusCode==400){
                return actionResult;
            }
            
            //2nd Email
            var password = GenerateRandomPassword(check);
            SendEmail(check, password);
            UpdatePasswordReset(check);
            
            //use different url 
            return Redirect("/LoginPage");
        }
        catch (Exception e) {
            return StatusCode(400, e.Message);
        }
    }

    private static void UpdatePasswordReset(PasswordReset check) {
        using var uow = new UnitOfWork();
        var record = uow.Query<PasswordReset>().Where(t => t.Oid == check.Oid)!.First();
        record.Status = Statuscode.Verified;
        uow.CommitChanges();
    }

    private static void SendEmail(PasswordReset check, string password) {
        //Send email here
    }

    private static string GenerateRandomPassword(PasswordReset check)
    {
        using var unitOWork = new UnitOfWork();
        var user = unitOWork.Query<ApplicationUser>().Where(t => t.Oid == check.User.Oid)!.First();
        var randomBytes = new byte[6];
        RandomNumberGenerator.Create().GetBytes(randomBytes);
        var password = Convert.ToBase64String(randomBytes);
        user.SetPassword(password);
        user.ChangePasswordOnFirstLogon = true;
        unitOWork.CommitChanges();
        return password;
    }

    private IActionResult Validations(PasswordReset check) {
        if (check == null) {
            return StatusCode(400, new { message = "Secret did not match." });
        }

        if (check.IsVerified) {
            return  StatusCode(400, new { message = "Link has been used." });
        }
            
        if (check.DropDead <= DateTime.Now) {
            return StatusCode(400, new { message = "Link is expired." });
        }

        return StatusCode(200, new { message = "Success" });
    }
}