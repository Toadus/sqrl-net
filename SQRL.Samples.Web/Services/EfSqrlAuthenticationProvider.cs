﻿using System;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using SQRL.Samples.Web.Models;
using SQRL.Server;

namespace SQRL.Samples.Web.Services
{
    public class EfSqrlAuthenticationProvider : ISqrlAuthenticationHandler
    {
        private const int TimeoutSeconds = 180;

        public void StartSession(string sessionId)
        {
            string httpSessionId = HttpContext.Current.Session.SessionID;
            using (var ctx = new UsersContext())
            {
                var session = ctx.UserSessions.Find(httpSessionId);
                if (session == null)
                {
                    session = new UserSession
                        {
                            SessionId = httpSessionId
                        };

                    ctx.UserSessions.Add(session);
                }

                session.AuthenticatedDatetime = null;
                session.SqrlId = sessionId;
                session.CreatedDatetime = DateTime.UtcNow;

                ctx.SaveChanges();
            }
        }

        public bool VerifySession(string ipAddress, string sessionId)
        {
            using (var ctx = new UsersContext())
            {
                DateTime timeout = DateTime.UtcNow.AddSeconds(-TimeoutSeconds);
                return ctx.UserSessions.Any(s => s.SqrlId == sessionId &&
                                                 s.AuthenticatedDatetime == null &&
                                                 s.CreatedDatetime >= timeout);
            }
        }

        public void AuthenticateSession(string userId, string ipAddress, string sessionId)
        {
            using (var ctx = new UsersContext())
            {
                DateTime timeout = DateTime.UtcNow.AddSeconds(-TimeoutSeconds);
                var usrSession = ctx.UserSessions.FirstOrDefault(s => s.SqrlId == sessionId &&
                                                                      s.AuthenticatedDatetime == null &&
                                                                      s.CreatedDatetime >= timeout);
                 if (usrSession == null)
                 {
                     return;
                 }

                usrSession.AuthenticatedDatetime = DateTime.UtcNow;
                usrSession.UserId = userId;

                ctx.SaveChanges();
            }

            var hubContext = GlobalHost.ConnectionManager.GetHubContext<LoginHub>();
            hubContext.Clients.Group(sessionId).login();
        }
    }

    public class EfSqrlAuthenticationProviderFactory : ISqrlAuthenticationHandlerFactory
    {
        public ISqrlAuthenticationHandler Create()
        {
            return new EfSqrlAuthenticationProvider();
        }
    }
}