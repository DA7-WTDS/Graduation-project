# QuantWise — LSTM backbone definition, shared by serving (main.py) and
# training/experiments. Must match the architecture the deployed
# models/lstm_backbone.pth weights were trained with.

from __future__ import annotations

import torch
import torch.nn as nn


class LSTMBackbone(nn.Module):
    def __init__(self, input_dim: int, hidden_dim: int, num_layers: int):
        super().__init__()
        self.hidden_dim = hidden_dim
        self.num_layers = num_layers
        self.lstm = nn.LSTM(
            input_size=input_dim,
            hidden_size=hidden_dim,
            num_layers=num_layers,
            batch_first=True,
            dropout=0.5 if num_layers > 1 else 0.0,
        )
        self.fc = nn.Linear(hidden_dim, 1)

    def forward(self, x: torch.Tensor):
        h0 = torch.zeros(self.num_layers, x.size(0), self.hidden_dim)
        c0 = torch.zeros(self.num_layers, x.size(0), self.hidden_dim)
        out, _ = self.lstm(x, (h0, c0))
        features   = out[:, -1, :]   # last time step — matches notebook (NOT h_n[-1])
        prediction = self.fc(features)
        return prediction, features
