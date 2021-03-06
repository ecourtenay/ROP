﻿using System;
using ROP.Example.Models;

namespace ROP.Example.Services
{
    public class TransferService : ITransferService
    {
        private readonly IAccountService _accountService;

        public TransferService(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public Func<TransferRequest, Result<TransferRequest, string>> CheckSufficientFunds =>
            request =>
            {
                var accountBalance = _accountService.GetAccountBalance(request.AccountFrom);
                return accountBalance >= request.TransferAmount
                    ? new Result<TransferRequest, string>.Success(request)
                    : new Result<TransferRequest, string>.Failure("Insufficient funds");
            };


        public Func<TransferRequest, Result<RingfencedTransferRequest, string>> RingfenceSourceAccount =>
            request =>
            {
                if (request.AccountFrom.StartsWith("8"))
                {
                    return new Result<RingfencedTransferRequest, string>.Failure($"Service timed out while attempting to ringfence {request.TransferAmount} from account {request.AccountFrom}");
                }

                var ringfencedRequest =
                    RingfencedTransferRequest.FromTransferRequest(request, Guid.NewGuid().ToString("N"));
                return new Result<RingfencedTransferRequest, string>.Success(ringfencedRequest);
            };

        public Func<RingfencedTransferRequest, Result<TransferResult, string>> TransferRingfencedAmount =>
            request =>
            {
                var (accountFrom, accountTo, transferAmount, reference, ringfenceReference) = request;
                if (!_accountService.TransferFunds(accountFrom, accountTo, transferAmount))
                {
                    return new Result<TransferResult, string>.Failure($"Network failure while attempting to fulfill ringfence {ringfenceReference} ");
                }

                var transferResult = new TransferResult(accountFrom, accountTo, transferAmount,
                    reference, ringfenceReference,
                    _accountService.GetAccountBalance(accountFrom),
                    _accountService.GetAccountBalance(accountTo));

                return new Result<TransferResult, string>.Success(transferResult);
            };
    }
}
