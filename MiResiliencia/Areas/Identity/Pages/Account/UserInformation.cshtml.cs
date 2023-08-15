// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using MiResiliencia.Models;

namespace MiResiliencia.Areas.Identity.Pages.Account
{
    public class UserInformationModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly ILogger<RegisterModel> _logger;

        public UserInformationModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     True if everything worked
        /// </summary>
        public bool TheResult { get; set; } 

        /// <summary>
        ///     The success message
        /// </summary>
        public string SuccessMessage { get; set; }
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [Display(ResourceType = typeof(MiResiliencia.Resources.Global), Name = "Email")]
            public string Email { get; set; }

            [Required]
            [Display(ResourceType = typeof(MiResiliencia.Resources.Global), Name = "Vorname")]
            public string FirstName { get; set; }
            [Required]
            [Display(ResourceType = typeof(MiResiliencia.Resources.Global), Name = "Nachname")]
            public string LastName { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "El {0} debe tener al menos {2} y como máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(ResourceType = typeof(MiResiliencia.Resources.Global), Name = "PasswortLabel")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(ResourceType = typeof(MiResiliencia.Resources.Global), Name = "ConfirmPassword")]
            [Compare("Password", ErrorMessage = "La clave de acceso y la clave de confirmación no coinciden.")]
            public string ConfirmPassword { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            var id = _userManager.GetUserId(User);
            var applicationUser = await _userManager.GetUserAsync(User);
            Input = new InputModel();
            Input.Email = applicationUser.Email;
            Input.FirstName = applicationUser.FirstName;
            Input.LastName = applicationUser.LastName; 

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                var id = _userManager.GetUserId(User);
                var applicationUser = await _userManager.GetUserAsync(User);

                applicationUser.Email = Input.Email;
                Input.FirstName = Input.FirstName;
                Input.LastName = Input.LastName;
                await _userManager.RemovePasswordAsync(applicationUser);
                await _userManager.AddPasswordAsync(applicationUser, Input.Password);
            }
            TheResult = true;
            SuccessMessage = "Clave cambiada con éxito";
            // If we got this far, something failed, redisplay form
            return Page();
        }

    }
}
