#region MIT License

// ==========================================================
// 
// MultiTenant_ForgotPassword project - Copyright (c) 2024 JeePeeTee
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

#region usings

using System.ComponentModel;
using DevExpress.Data.Filtering;
using DevExpress.Data.ODataLinq.Helpers;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using DevExpress.XtraPrinting.Native;

#endregion

namespace XafPasswordReset.Module.BusinessObjects;

[DefaultClassOptions]
[ImageName("BO_Contact")]
[DefaultProperty(nameof(PasswordReset.DefaultProperty))]
[CreatableItem(false)]
public class PasswordReset : BaseObject {
    
    private DateTime _requestDate;
    private DateTime _confirmationDate;
    
    private ApplicationUser _user;
    private Statuscode _statuscode;

    public PasswordReset(Session session) : base(session) { }

    [RuleRequiredField("RuleRequiredField for PasswordReset.User", DefaultContexts.Save, "User is verplicht!")]
    public ApplicationUser User {
        get => _user;
        set => SetPropertyValue(nameof(User), ref _user, value);
    }

    public DateTime RequestDate {
        get => _requestDate;
        set => SetPropertyValue(nameof(RequestDate), ref _requestDate, value);
    }
    
    public DateTime ConfirmationDate {
        get => _confirmationDate;
        set => SetPropertyValue(nameof(ConfirmationDate), ref _confirmationDate, value);
    }
    
    public Statuscode Status {
        get => _statuscode;
        set => SetPropertyValue(nameof(Status), ref _statuscode, value);
    }

    public Boolean IsVerified => Status == Statuscode.Verified;
    
    private DateTime _dropDead;

    public DateTime DropDead {
        get => _dropDead;
        set => SetPropertyValue(nameof(DropDead), ref _dropDead, value);
    }

    [VisibleInListView(false)]
    [VisibleInDetailView(false)]
    public string DefaultProperty => "Default Property";

    protected override void OnChanged(string propertyName, object oldValue, object newValue) {
        base.OnChanged(propertyName, oldValue, newValue);
        switch (propertyName) {
            case nameof(RequestDate):
                DropDead = ((DateTime)newValue).AddDays(14);
                break;
            case nameof(Status): {
                if (((Statuscode)newValue) == Statuscode.Verified) {
                    ConfirmationDate = DateTime.Now;
                }
                if (((Statuscode)newValue) == Statuscode.Unverified) {
                    ConfirmationDate = DateTime.MinValue;
                }
                break;
            }
        }
    }

    protected override void OnSaving() {
        base.OnSaving();
        // Invalidate all other PasswordReset objects for this user
        //if (Session.IsNewObject(this)) {
            Session.Query<PasswordReset>().Where(w => 
                w.User == this.User &&
                w.Status == Statuscode.Unverified &&
                w.Oid != this.Oid).ToList().ForEach(w=>w.Status = Statuscode.Invalid);
        //}
    }

    public override void AfterConstruction() {
        base.AfterConstruction();
        RequestDate = DateTime.Now;
        Status = Statuscode.Unverified;
    }
}

public enum Statuscode
{
    Unverified,
    Verified,
    Invalid
}
