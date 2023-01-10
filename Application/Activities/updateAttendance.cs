using MediatR;
using System;
using Application.Core;
using Application.Interfaces;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Domain;

namespace Application.Activities
{
    public class updateAttendance
    {
        public class Command : IRequest<Result<Unit>>
        {
            public Guid Id { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result<Unit>>
        {
            public DataContext _context { get; }
            public IUserAccessor _userAccessor { get; }
            public Handler(DataContext context, IUserAccessor userAccessor)
            {
                _userAccessor = userAccessor;
                _context = context;
            }

            public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
            {
                var activity = await _context.Activities
                    .Include(a=> a.Attendees).ThenInclude(u => u.AppUser)
                    .SingleOrDefaultAsync(x => x.Id == request.Id);

                if (activity == null) return null;

                var user = await _context.Users.FirstOrDefaultAsync(x=>
                x.UserName == _userAccessor.GetUsername());

                if (user==null) return null;

                var HostUsername = activity.Attendees.FirstOrDefault(x=>x.IsHost)?.AppUser?.UserName;

                var attendance = activity.Attendees.FirstOrDefault(x=>x.AppUser.UserName == user.UserName);

                if(attendance != null && HostUsername == user.UserName)
                    activity.isCancelled=!activity.isCancelled;

                if (attendance!=null && HostUsername !=user.UserName)
                    activity.Attendees.Remove(attendance);

                if (attendance ==null)
                {
                    attendance=new ActivityAttendee
                    {
                        AppUser = user,
                        Activity = activity,
                        IsHost = false
                    };

                    activity.Attendees.Add(attendance);


                }

                var Result = await _context.SaveChangesAsync() >0;

                return Result ? Result<Unit>.Success(Unit.Value) : Result<Unit>.Failure("Problem updating attendance!!");
            }
        }
    }
}