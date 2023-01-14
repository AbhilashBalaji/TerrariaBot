library(tidyverse)      # import ggplot, dplyr, readr, etc.
library(data.table)     # data analysis
library(zoo)            # for working with time series data
library(magrittr)

library(coronavirus)    # dataset for example plots

theme_set(theme_bw())   # set black and white theme for plots.
library(gghighlight)    # for highlighting in plots
library(cowplot)        # convenience functions for plotting

library(knitr)          # literate programming library



# load data set
output %>%
  filter(Latency < 12500000) %>% ##Filter out outliers
  filter(Botcount %% 5 == 0) %>%
  filter(Botcount > 0) %>%
  mutate(Botcount = factor(Botcount, levels=c(5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100, 105))) %>% #change these intervals to be what you need
  # plot data. 'fill=Species' adds the different colors
  ggplot(aes(y=Latency, x=Botcount)) +
  # create box plot
  geom_boxplot() +
  # calculate the arithmetic mean, for each species. Show in plot using a white circle with black border
  stat_summary(fun=mean, geom="point", shape=21, size=2, color="black", fill="white") +
  # add pretty labels
  labs(x= "", y="Time [ns]") +
  # set plot theme
  theme_half_open() +
  # add major grid lines for horizontal axis
  background_grid(major = "x") +
  # remove legend
  theme(axis.text=element_text(size=18), axis.title=element_text(size=18), legend.position = "none") +
  #scale_x_continuous(breaks=c(5, 10, 15, 20, 25, 30, 35), limits = c(0, NA)) +

  # make boxes horizontal, not vertical
  coord_flip()

