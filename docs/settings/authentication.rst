.. _refAuthentication:
##############
Authentication
##############

SegnoSharp is set up to use `OpenID Connect <https://openid.net/developers/how-connect-works/>`_ (OIDC) for authentication and authorization.
The preferred OIDC provider must be set up to allow SegnoSharp to authenticate through it.
This entails creating a "Client" with a "Client ID" and "Client Secret", and configuring SegnoSharp to use these values.

.. note:: This documentation will not cover how to create an OIDC provider or how to configure the provider, but all the major computer companies like Google, Microsoft, and Meta all have their own OIDC providers you can create your own clients with.

When you have the client values use them in the following configuration options in SegnoSharp:

+----------------------------------+-----------------------+------------------------------------------------------------------------------------------------------------------------------------+
| OpenIdConnect.Authority          | URI                   | OIDC provider URI, i.e. ``https://accounts.google.com/``.                                                                          |
+----------------------------------+-----------------------+------------------------------------------------------------------------------------------------------------------------------------+
| OpenIdConnect.ClientId           | Secret string value   | "ClientId" defined in the OIDC provider.                                                                                           |
+----------------------------------+-----------------------+------------------------------------------------------------------------------------------------------------------------------------+
| OpenIdConnect.ClientSecret       | Secret string value   | "ClientSecret" defined in the OIDC provider.                                                                                       |
+----------------------------------+-----------------------+------------------------------------------------------------------------------------------------------------------------------------+
| OpenIdConnect.AdditionalScopes   | Secret string value   | SegnoSharp will always ask for ``openid`` and ``profile`` scopes. If you need additional scopes add them here.                     |
+----------------------------------+-----------------------+------------------------------------------------------------------------------------------------------------------------------------+
| OpenIdConnect.AdminClaimKey      | Secret string value   | Of all the claims returned from the OIDC provider, which claim key should be checked to determine administration privileges.       |
+----------------------------------+-----------------------+------------------------------------------------------------------------------------------------------------------------------------+
| OpenIdConnect.AdminClaimValue    | Secret string value   | If the value of the claim defined above contains this value, then the authentication is authorized with administration privileges. |
+----------------------------------+-----------------------+------------------------------------------------------------------------------------------------------------------------------------+
| OpenIdConnect.UsernameClaimKey   | Secret string value   | Of all the claims returned from the OIDC provider, which claim key will be mapped to the username displayed in SegnoSharp          |
+----------------------------------+-----------------------+------------------------------------------------------------------------------------------------------------------------------------+
| OpenIdConnect.SupportsEndSession | Secret string value   | Whether the OIDC provider supports the "end_session_endpoint". Default ``false``.                                                  |
+----------------------------------+-----------------------+------------------------------------------------------------------------------------------------------------------------------------+
| OpenIdConnect.UseOidc            | ``true`` or ``false`` | Whether to use OIDC or not. Default ``true``. See below!                                                                           |
+----------------------------------+-----------------------+------------------------------------------------------------------------------------------------------------------------------------+

.. note:: ``UseOidc`` should never be set to ``false`` in a production environment! Setting this to false overrides all security measures and allows anyone to log on as administrator!

.. note:: Check the OIDC provider's ``.well-known/openid-configuration`` to see if it supports the "end_session_endpoint".
