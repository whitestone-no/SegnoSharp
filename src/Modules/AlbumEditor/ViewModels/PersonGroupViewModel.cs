using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Modules.AlbumEditor.ViewModels
{
    public class PersonGroupViewModel : PersonGroup
    {
        private readonly SegnoSharpDbContext _dbContext;

        public PersonGroupViewModel(SegnoSharpDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public bool IncludeInAutoPlaylist
        {
            get => PersonGroupStreamInfo is { IncludeInAutoPlaylist: true };
            set
            {
                if (PersonGroupStreamInfo == null)
                {
                    PersonGroupStreamInfo = new PersonGroupStreamInfo
                    {
                        PersonGroupId = Id
                    };
                    _dbContext.PersonGroupsStreamInfos.Add(PersonGroupStreamInfo);
                }

                PersonGroupStreamInfo.IncludeInAutoPlaylist = value;
            }
        }
    }
}
