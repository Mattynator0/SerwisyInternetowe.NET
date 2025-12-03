// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/token/ERC20/ERC20.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

contract SensorToken is ERC20, Ownable {
    constructor() ERC20("Sensor Token", "STKN") Ownable(msg.sender) {}

    /// @notice Nagradza sensor tokenami (mint nowych tokenów)
    /// @param sensor adres portfela sensora
    /// @param amount iloœæ tokenów (w najmniejszych jednostkach, np. 1e18 = 1 STKN)
    function rewardSensor(address sensor, uint256 amount) external onlyOwner {
        _mint(sensor, amount);
    }
}
