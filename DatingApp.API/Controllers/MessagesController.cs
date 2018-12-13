using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Contexts;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
  [ServiceFilter(typeof(LogUserActivity))]
  [Authorize]
  [Route("api/users/{userId}/[controller]")]
  [ApiController]
  public class MessagesController : ControllerBase
  {
    private readonly IDatingRepository _repo;
    private readonly IMapper _mapper;
    public MessagesController(IDatingRepository repo, IMapper mapper)
    {
      this._repo = repo;
      this._mapper = mapper;
    }

    [HttpGet("{id}", Name = "GetMessage")]
    public async Task<IActionResult> GetMessage(int userid, int id)
    {
      if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
      {
        return Unauthorized();
      }

      var messageFromRepo = await _repo.GetMessage(id);

      if (messageFromRepo == null)
      {
        return NotFound();
      }

      return Ok(messageFromRepo);
    }

    [HttpGet("thread/{recipientId}")]
    public async Task<IActionResult> GetMessageThread(int userId, int recipientId)
    {
      if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
      {
        return Unauthorized();
      }

      var messageFromRepo = await _repo.GetMessageThread(userId, recipientId);

      var messageThread = _mapper.Map<IEnumerable<MessageToReturnDto>>(messageFromRepo);

      return Ok(messageThread);
    }

    [HttpGet]
    public async Task<IActionResult> GetMessagesForUser(int userId, [FromQuery]MessageParams messageParams)
    {
      if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
      {
        return Unauthorized();
      }

      messageParams.UserId = userId;

      var messagesFromRepo = await _repo.GetMessagesForUser(messageParams);

      var messages = _mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);

      Response.AddPagination(messagesFromRepo.CurrentPage, messagesFromRepo.PageSize, 
        messagesFromRepo.TotalCount, messagesFromRepo.TotalPages);

      return Ok(messages);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMessage(int userId, MessageForCreationDto messageForCreationDto)
    {
      if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
      {
        return Unauthorized();
      }

      messageForCreationDto.SenderId = userId;

      var recipient = await _repo.GetUser(messageForCreationDto.RecipientId);

      if (recipient == null)
      {
        return BadRequest("Could not find user");
      }

      var message = _mapper.Map<Message>(messageForCreationDto);

      _repo.Add(message);

      var messageToReturn = _mapper.Map<MessageForCreationDto>(message);

      if (await _repo.SaveAll())
      {
        return CreatedAtRoute("GetMessage", new { id = message.Id }, messageToReturn);
      }

      throw new Exception("Creating the message failed on save");
    }
  }
}