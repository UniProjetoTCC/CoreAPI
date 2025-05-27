using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class BackgroundJobRepository : IBackgroundJobRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public BackgroundJobRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<HangJob> CreateAsync(HangJob job)
        {
            var model = _mapper.Map<BackgroundJobsModel>(job);
            model.CreatedAt = DateTime.UtcNow;
            model.Status = "Scheduled";

            _context.BackgroundJobs.Add(model);
            await _context.SaveChangesAsync();

            return _mapper.Map<HangJob>(model);
        }

        public async Task<HangJob?> GetByHangfireJobIdAsync(string hangfireJobId)
        {
            var model = await _context.BackgroundJobs
                .FirstOrDefaultAsync(j => j.HangfireJobId == hangfireJobId);

            return model != null ? _mapper.Map<HangJob>(model) : null;
        }

        public async Task<IEnumerable<HangJob>?> GetActiveJobsByGroupIdAsync(string groupId)
        {
            var models = await _context.BackgroundJobs
                .Where(j => j.GroupId == groupId && j.Status == "Scheduled")
                .ToListAsync();

            if (models == null || !models.Any())
                return null;

            return _mapper.Map<IEnumerable<HangJob>>(models);
        }

        public async Task<HangJob?> UpdateAsync(HangJob job)
        {
            var model = await _context.BackgroundJobs.FindAsync(job.Id);
            if (model == null)
                return null;

            _mapper.Map(job, model);
            _context.BackgroundJobs.Update(model);
            await _context.SaveChangesAsync();

            return _mapper.Map<HangJob>(model);
        }
    }
}
